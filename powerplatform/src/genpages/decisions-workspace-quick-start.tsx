import * as React from "react";

type Section = {
  title: string;
  summary: string;
  items: string[];
};

const navigationSections: Section[] = [
  {
    title: "Meetings",
    summary: "Meeting list, meeting workspace, agenda, attendance, and papers.",
    items: [
      "Start from the meeting list, then open the meeting workspace as the active record chain.",
      "Build the agenda in order, confirm attendance, and link papers to the right agenda items.",
      "Keep the meeting as the parent context while you move through drafting and review."
    ]
  },
  {
    title: "Governance",
    summary: "Bodies, meeting series, terms of reference, memberships, people, and reporting lines.",
    items: [
      "Create or confirm the governance body, committee subtype, terms of reference, and meeting series.",
      "Review membership, org hierarchy, and access rules before the meeting is opened for work.",
      "Use governance maintenance when the selected body or series needs to change."
    ]
  },
  {
    title: "Actions",
    summary: "Follow-up actions, ownership, due dates, escalation, and Planner binding state.",
    items: [
      "Use the decisions and actions screen to keep action ownership and due dates current.",
      "Treat Planner sync as deferred until the binding is explicitly enabled.",
      "Keep escalation and status updates in the app until an external task binding is ready."
    ]
  },
  {
    title: "Decisions",
    summary: "Minutes, formal decisions, votes, and publication review.",
    items: [
      "Draft minutes from the transcript or meeting record, then review the resulting decisions and votes.",
      "Publish only after the approval and human-review rules are satisfied.",
      "Use the meeting workspace to move between draft, review, and published states."
    ]
  },
  {
    title: "Papers",
    summary: "Paper register, attachments, and transcript-linked supporting material.",
    items: [
      "Register papers from the selected meeting or agenda item and keep the attachment source explicit.",
      "Use the SharePoint document store as a deferred binding, not a runtime dependency.",
      "Link papers to agenda items so the meeting chain stays traceable."
    ]
  },
  {
    title: "Help",
    summary: "Quick-start guidance, role-based first steps, and source rules.",
    items: [
      "Use this guide as the Help surface for the app shell and home-page contract.",
      "The sections below are expandable so the guide mirrors the collapsible navigation model.",
      "Real committee records must come from NSW Outlook, SharePoint, Teams, or user-provided exported files."
    ]
  }
];

const roleSteps = [
  {
    title: "Chair",
    body: "Review agenda items, papers, conflicts, quorum, decisions, actions, and draft minutes. Approve official minutes only after secretariat review."
  },
  {
    title: "Secretariat",
    body: "Maintain meeting records, attendees, agenda, papers, minutes, decisions, and action follow-up. Import transcripts or notes only from approved NSW sources."
  },
  {
    title: "Member",
    body: "Review agenda, papers, minutes, decisions, and assigned actions. Update action status and disclose conflicts through the agreed workflow."
  },
  {
    title: "Administrator",
    body: "Configure feature switches, access rules, connector settings, and integration health monitoring."
  }
];

const palette = {
  text: "#1f1f1f",
  muted: "#5f5f5f",
  border: "#d7dbe0",
  surface: "#ffffff",
  soft: "#f5f7fa",
  accent: "#005fb8",
  danger: "#a4262c"
};

export default function DecisionsWorkspaceQuickStart(): JSX.Element {
  return (
    <main style={styles.page}>
      <header style={styles.header}>
        <div>
          <p style={styles.kicker}>Help</p>
          <h1 style={styles.h1}>Decisions Workspace Quick Start</h1>
          <p style={styles.lead}>
            Use this guide to navigate the app shell, start from Workspace Home, and move through meetings, governance,
            decisions, actions, and papers without losing the record chain.
          </p>
        </div>
      </header>

      <section style={styles.warning}>
        <strong>Source rule:</strong> real Medication Safety Committee records must come from NSW Outlook, SharePoint,
        Teams, or user-provided exported files.
      </section>

      <section style={styles.detailsStack}>
        {navigationSections.map((section) => (
          <details key={section.title} style={styles.details}>
            <summary style={styles.summary}>
              <span style={styles.summaryTitle}>{section.title}</span>
              <span style={styles.summaryText}>{section.summary}</span>
            </summary>
            <ul style={styles.list}>
              {section.items.map((item) => (
                <li key={item} style={styles.item}>
                  {item}
                </li>
              ))}
            </ul>
          </details>
        ))}
      </section>

      <section style={styles.roleSection}>
        <h2 style={styles.h2}>Role-based first steps</h2>
        <div style={styles.roleGrid}>
          {roleSteps.map((role) => (
            <article key={role.title} style={styles.roleCard}>
              <h3 style={styles.h3}>{role.title}</h3>
              <p style={styles.roleBody}>{role.body}</p>
            </article>
          ))}
        </div>
      </section>
    </main>
  );
}

const styles: Record<string, React.CSSProperties> = {
  page: {
    boxSizing: "border-box",
    padding: "28px",
    color: palette.text,
    background: palette.surface,
    fontFamily: "Segoe UI, Arial, sans-serif",
    lineHeight: 1.45,
    maxWidth: "1120px"
  },
  header: {
    marginBottom: "16px"
  },
  kicker: {
    margin: "0 0 6px",
    color: palette.accent,
    fontSize: "12px",
    fontWeight: 700,
    letterSpacing: 0,
    textTransform: "uppercase"
  },
  h1: {
    margin: 0,
    fontSize: "28px",
    fontWeight: 600
  },
  lead: {
    margin: "8px 0 0",
    color: palette.muted,
    fontSize: "15px",
    maxWidth: "760px"
  },
  warning: {
    borderLeft: `4px solid ${palette.danger}`,
    background: "#fff6f7",
    padding: "12px 14px",
    marginBottom: "18px",
    borderRadius: "4px"
  },
  detailsStack: {
    display: "grid",
    gap: "10px"
  },
  details: {
    border: `1px solid ${palette.border}`,
    borderRadius: "6px",
    background: palette.surface,
    overflow: "hidden"
  },
  summary: {
    listStyle: "none",
    cursor: "pointer",
    display: "grid",
    gap: "2px",
    padding: "14px 16px",
    background: palette.soft
  },
  summaryTitle: {
    fontSize: "16px",
    fontWeight: 600
  },
  summaryText: {
    fontSize: "13px",
    color: palette.muted
  },
  list: {
    margin: 0,
    padding: "12px 16px 14px 34px"
  },
  item: {
    marginBottom: "8px"
  },
  roleSection: {
    marginTop: "22px",
    borderTop: `1px solid ${palette.border}`,
    paddingTop: "18px"
  },
  h2: {
    margin: "0 0 10px",
    fontSize: "18px",
    fontWeight: 600
  },
  roleGrid: {
    display: "grid",
    gridTemplateColumns: "repeat(auto-fit, minmax(220px, 1fr))",
    gap: "12px"
  },
  roleCard: {
    background: palette.soft,
    border: `1px solid ${palette.border}`,
    borderRadius: "6px",
    padding: "12px"
  },
  h3: {
    margin: "0 0 6px",
    fontSize: "15px",
    fontWeight: 600
  },
  roleBody: {
    margin: 0,
    color: palette.muted
  }
};
