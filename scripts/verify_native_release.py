"""Verify the actual library/source archive against immutable Git blob bytes."""
import argparse
import hashlib
import io
import json
import subprocess
import zipfile
from pathlib import Path, PurePosixPath


def git_blobs(root, commit):
    rows = subprocess.check_output(["git", "-C", str(root), "ls-tree", "-rz", commit]).split(b"\0")
    blobs = []
    for row in filter(None, rows):
        metadata, name = row.split(b"\t", 1)
        mode, kind, oid = metadata.split()
        if kind == b"blob":
            if mode == b"120000":
                raise ValueError("Source symlinks require a separately qualified export contract")
            blobs.append((name.decode("utf-8"), oid))
    result = subprocess.run(
        ["git", "-C", str(root), "cat-file", "--batch"],
        input=b"\n".join(oid for _, oid in blobs) + b"\n", capture_output=True, check=True,
    )
    stream = io.BytesIO(result.stdout)
    contents = {}
    for name, expected in blobs:
        oid, kind, length = stream.readline().split()
        if oid != expected or kind != b"blob":
            raise ValueError("Unexpected Git object in source inventory")
        contents[name] = stream.read(int(length))
        if stream.read(1) != b"\n":
            raise ValueError("Truncated Git source inventory")
    return contents


def members(archive):
    entries = [i for i in archive.infolist() if not i.is_dir()]
    if len({i.filename for i in entries}) != len(entries) or archive.testzip() is not None:
        raise ValueError("Archive has duplicate members or failed CRC")
    for entry in entries:
        path = PurePosixPath(entry.filename)
        if path.is_absolute() or ".." in path.parts or (entry.external_attr >> 16) & 0o170000 == 0o120000:
            raise ValueError("Unsafe archive member")
    return {i.filename: archive.read(i) for i in entries}


def verify(archive_path, source_commit, root):
    expected = git_blobs(root, source_commit)
    with zipfile.ZipFile(archive_path) as archive:
        payload = members(archive)
    manifest = json.loads(payload["native-release.json"])
    if manifest["sourceCommit"] != source_commit:
        raise ValueError("Release source identity mismatch")
    with zipfile.ZipFile(io.BytesIO(payload["source.zip"])) as source:
        actual = members(source)
    if actual != expected:
        changed = sorted(name for name in actual.keys() | expected.keys() if actual.get(name) != expected.get(name))
        raise ValueError("Source archive differs from committed blob bytes: " + ", ".join(changed[:8]))
    for name in ("LICENSE", "global.json"):
        if payload[name] != expected[name]:
            raise ValueError("Release copy differs from committed " + name)
    required = {"CareOps.Decisions." + name for name in ("Authority", "Calendars", "RegisterSchema", "DataverseIdentity")}
    if len(manifest["assemblies"]) != 4 or {a["name"] for a in manifest["assemblies"]} != required:
        raise ValueError("Native library set is incomplete")
    for assembly in manifest["assemblies"]:
        data = payload["lib/" + assembly["name"] + ".dll"]
        if not data.startswith(b"MZ") or hashlib.sha256(data).hexdigest() != assembly["sha256"]:
            raise ValueError("Native library bytes differ from the release manifest")
    return {"sourceCommit": source_commit, "files": len(payload), "sourceFiles": len(actual)}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--archive", required=True, type=Path)
    parser.add_argument("--source-commit", required=True)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    print(json.dumps(verify(args.archive, args.source_commit, args.root)))
