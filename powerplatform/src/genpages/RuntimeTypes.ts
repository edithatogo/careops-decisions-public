// ---------------- Type Definitions which can be imported from ./RuntimeTypes -------------------------
export interface TableRegistrations extends BaseTableRegistrations {
    "dft_actionitem": dft_actionitem,
    "dft_agendaitem": dft_agendaitem,
    "dft_committeemembership": dft_committeemembership,
    "dft_decision": dft_decision,
    "dft_governancebody": dft_governancebody,
    "dft_meeting": dft_meeting,
    "dft_meetingfeatureoverride": dft_meetingfeatureoverride,
    "dft_meetingseries": dft_meetingseries,
    "dft_minutes": dft_minutes,
    "dft_paper": dft_paper,
    "dft_termsofreference": dft_termsofreference,
}
export interface EnumRegistrations extends BaseEnumRegistrations {
    "dft_actionitem-dft_prioritycode": dft_actionitem_dft_prioritycode,
    "dft_actionitem-dft_statuscode": dft_actionitem_dft_statuscode,
    "dft_actionitem-statecode": dft_actionitem_statecode,
    "dft_actionitem-statuscode": dft_actionitem_statuscode,
    "dft_agendaitem-dft_decisionsought": dft_agendaitem_dft_decisionsought,
    "dft_agendaitem-dft_purposecode": dft_agendaitem_dft_purposecode,
    "dft_agendaitem-dft_statuscode": dft_agendaitem_dft_statuscode,
    "dft_agendaitem-statecode": dft_agendaitem_statecode,
    "dft_agendaitem-statuscode": dft_agendaitem_statuscode,
    "dft_committeemembership-dft_isalternate": dft_committeemembership_dft_isalternate,
    "dft_committeemembership-dft_membershiprolecode": dft_committeemembership_dft_membershiprolecode,
    "dft_committeemembership-dft_membershipstatuscode": dft_committeemembership_dft_membershipstatuscode,
    "dft_committeemembership-dft_votingrights": dft_committeemembership_dft_votingrights,
    "dft_committeemembership-statecode": dft_committeemembership_statecode,
    "dft_committeemembership-statuscode": dft_committeemembership_statuscode,
    "dft_decision-dft_statuscode": dft_decision_dft_statuscode,
    "dft_decision-statecode": dft_decision_statecode,
    "dft_decision-statuscode": dft_decision_statuscode,
    "dft_governancebody-dft_bodytypecode": dft_governancebody_dft_bodytypecode,
    "dft_governancebody-dft_defaultclassificationcode": dft_governancebody_dft_defaultclassificationcode,
    "dft_governancebody-dft_statuscode": dft_governancebody_dft_statuscode,
    "dft_governancebody-statecode": dft_governancebody_statecode,
    "dft_governancebody-statuscode": dft_governancebody_statuscode,
    "dft_meeting-dft_classificationcode": dft_meeting_dft_classificationcode,
    "dft_meeting-dft_meetingstatuscode": dft_meeting_dft_meetingstatuscode,
    "dft_meeting-statecode": dft_meeting_statecode,
    "dft_meeting-statuscode": dft_meeting_statuscode,
    "dft_meetingfeatureoverride-dft_statecode": dft_meetingfeatureoverride_dft_statecode,
    "dft_meetingfeatureoverride-statecode": dft_meetingfeatureoverride_statecode,
    "dft_meetingfeatureoverride-statuscode": dft_meetingfeatureoverride_statuscode,
    "dft_meetingseries-dft_meetingcadencecode": dft_meetingseries_dft_meetingcadencecode,
    "dft_meetingseries-dft_seriesstatuscode": dft_meetingseries_dft_seriesstatuscode,
    "dft_meetingseries-statecode": dft_meetingseries_statecode,
    "dft_meetingseries-statuscode": dft_meetingseries_statuscode,
    "dft_minutes-dft_classificationcode": dft_minutes_dft_classificationcode,
    "dft_minutes-dft_statuscode": dft_minutes_dft_statuscode,
    "dft_minutes-statecode": dft_minutes_statecode,
    "dft_minutes-statuscode": dft_minutes_statuscode,
    "dft_paper-dft_classificationcode": dft_paper_dft_classificationcode,
    "dft_paper-dft_documentsourcecode": dft_paper_dft_documentsourcecode,
    "dft_paper-dft_paperstatuscode": dft_paper_dft_paperstatuscode,
    "dft_paper-statecode": dft_paper_statecode,
    "dft_paper-statuscode": dft_paper_statuscode,
    "dft_termsofreference-dft_approvalstatuscode": dft_termsofreference_dft_approvalstatuscode,
    "dft_termsofreference-dft_quorumdenominatorcode": dft_termsofreference_dft_quorumdenominatorcode,
    "dft_termsofreference-dft_statuscode": dft_termsofreference_dft_statuscode,
    "dft_termsofreference-statecode": dft_termsofreference_statecode,
    "dft_termsofreference-statuscode": dft_termsofreference_statuscode,
}
export type dft_actionitem = TableRow<{
    // Primary Key Column
    readonly dft_actionitemid: string,
    readonly createdbyname: string,
    readonly createdbyyominame: string,
    readonly createdonbehalfbyname: string,
    readonly createdonbehalfbyyominame: string,
    dft_actionnumber: string,
    dft_actionsummary: string,
    // Foreign Key Column
    _dft_agendaitemid_value: `/dft_agendaitem(${string})`,
    readonly dft_agendaitemidname: string,
    dft_completionevidence: string,
    // Foreign Key Column
    _dft_decisionid_value: `/dft_decision(${string})`,
    readonly dft_decisionidname: string,
    dft_duedate: Date,
    // Foreign Key Column
    _dft_meetingid_value: `/dft_meeting(${string})`,
    readonly dft_meetingidname: string,
    dft_name: string,
    // Foreign Key Column
    readonly _dft_ownerid_value: `/dft_orgperson(${string})`,
    readonly dft_owneridname: string,
    dft_plannertaskid: string,
    dft_prioritycode: dft_actionitem_dft_prioritycode,
    dft_statuscode: dft_actionitem_dft_statuscode,
    readonly modifiedbyname: string,
    readonly modifiedbyyominame: string,
    readonly modifiedonbehalfbyname: string,
    readonly modifiedonbehalfbyyominame: string,
    readonly owningbusinessunitname: string,
    statecode: dft_actionitem_statecode,
    statuscode: dft_actionitem_statuscode,
}>

export type dft_agendaitem = TableRow<{
    // Primary Key Column
    readonly dft_agendaitemid: string,
    readonly createdbyname: string,
    readonly createdbyyominame: string,
    readonly createdonbehalfbyname: string,
    readonly createdonbehalfbyyominame: string,
    dft_agendatitle: string,
    dft_decisionsought: dft_agendaitem_dft_decisionsought,
    // Foreign Key Column
    _dft_meetingid_value: `/dft_meeting(${string})`,
    readonly dft_meetingidname: string,
    dft_name: string,
    // Foreign Key Column
    readonly _dft_presenterid_value: `/dft_orgperson(${string})`,
    readonly dft_presenteridname: string,
    dft_purposecode: dft_agendaitem_dft_purposecode,
    dft_sequence: number,
    dft_statuscode: dft_agendaitem_dft_statuscode,
    dft_timeminutes: number,
    readonly modifiedbyname: string,
    readonly modifiedbyyominame: string,
    readonly modifiedonbehalfbyname: string,
    readonly modifiedonbehalfbyyominame: string,
    readonly owningbusinessunitname: string,
    statecode: dft_agendaitem_statecode,
    statuscode: dft_agendaitem_statuscode,
}>

export type dft_committeemembership = TableRow<{
    // Primary Key Column
    readonly dft_committeemembershipid: string,
    readonly createdbyname: string,
    readonly createdbyyominame: string,
    readonly createdonbehalfbyname: string,
    readonly createdonbehalfbyyominame: string,
    dft_enddate: Date,
    // Foreign Key Column
    _dft_governancebodyid_value: `/dft_governancebody(${string})`,
    readonly dft_governancebodyidname: string,
    dft_isalternate: dft_committeemembership_dft_isalternate,
    dft_membershiprolecode: dft_committeemembership_dft_membershiprolecode,
    dft_membershipstatuscode: dft_committeemembership_dft_membershipstatuscode,
    dft_name: string,
    // Foreign Key Column
    readonly _dft_orgpersonid_value: `/dft_orgperson(${string})`,
    readonly dft_orgpersonidname: string,
    dft_startdate: Date,
    dft_votingrights: dft_committeemembership_dft_votingrights,
    readonly modifiedbyname: string,
    readonly modifiedbyyominame: string,
    readonly modifiedonbehalfbyname: string,
    readonly modifiedonbehalfbyyominame: string,
    readonly owningbusinessunitname: string,
    statecode: dft_committeemembership_statecode,
    statuscode: dft_committeemembership_statuscode,
}>

export type dft_decision = TableRow<{
    // Primary Key Column
    readonly dft_decisionid: string,
    readonly createdbyname: string,
    readonly createdbyyominame: string,
    readonly createdonbehalfbyname: string,
    readonly createdonbehalfbyyominame: string,
    // Foreign Key Column
    _dft_agendaitemid_value: `/dft_agendaitem(${string})`,
    readonly dft_agendaitemidname: string,
    dft_authority: string,
    dft_decisionnumber: string,
    dft_decisiontext: string,
    dft_effectiveon: Date,
    // Foreign Key Column
    _dft_meetingid_value: `/dft_meeting(${string})`,
    readonly dft_meetingidname: string,
    dft_name: string,
    // Foreign Key Column
    readonly _dft_ownerid_value: `/dft_orgperson(${string})`,
    readonly dft_owneridname: string,
    dft_reviewdueon: Date,
    dft_statuscode: dft_decision_dft_statuscode,
    readonly modifiedbyname: string,
    readonly modifiedbyyominame: string,
    readonly modifiedonbehalfbyname: string,
    readonly modifiedonbehalfbyyominame: string,
    readonly owningbusinessunitname: string,
    statecode: dft_decision_statecode,
    statuscode: dft_decision_statuscode,
}>

export type dft_governancebody = TableRow<{
    // Primary Key Column
    readonly dft_governancebodyid: string,
    readonly createdbyname: string,
    readonly createdbyyominame: string,
    readonly createdonbehalfbyname: string,
    readonly createdonbehalfbyyominame: string,
    dft_bodycode: string,
    dft_bodytypecode: dft_governancebody_dft_bodytypecode,
    dft_defaultclassificationcode: dft_governancebody_dft_defaultclassificationcode,
    dft_description: string,
    dft_name: string,
    // Foreign Key Column
    readonly _dft_ownerpersonid_value: `/dft_orgperson(${string})`,
    readonly dft_ownerpersonidname: string,
    // Foreign Key Column
    _dft_parentgovernancebodyid_value: `/dft_governancebody(${string})`,
    readonly dft_parentgovernancebodyidname: string,
    dft_statuscode: dft_governancebody_dft_statuscode,
    readonly modifiedbyname: string,
    readonly modifiedbyyominame: string,
    readonly modifiedonbehalfbyname: string,
    readonly modifiedonbehalfbyyominame: string,
    readonly owningbusinessunitname: string,
    statecode: dft_governancebody_statecode,
    statuscode: dft_governancebody_statuscode,
}>

export type dft_meeting = TableRow<{
    // Primary Key Column
    readonly dft_meetingid: string,
    readonly createdbyname: string,
    readonly createdbyyominame: string,
    readonly createdonbehalfbyname: string,
    readonly createdonbehalfbyyominame: string,
    dft_actualendutc: Date,
    dft_actualstartutc: Date,
    // Foreign Key Column
    readonly _dft_chairpersonid_value: `/dft_orgperson(${string})`,
    readonly dft_chairpersonidname: string,
    dft_classificationcode: dft_meeting_dft_classificationcode,
    dft_externalcalendarid: string,
    dft_externalteamsid: string,
    dft_location: string,
    dft_meetingnumber: number,
    // Foreign Key Column
    _dft_meetingseriesid_value: `/dft_meetingseries(${string})`,
    readonly dft_meetingseriesidname: string,
    dft_meetingstatuscode: dft_meeting_dft_meetingstatuscode,
    dft_name: string,
    dft_scheduledendutc: Date,
    dft_scheduledstartutc: Date,
    readonly modifiedbyname: string,
    readonly modifiedbyyominame: string,
    readonly modifiedonbehalfbyname: string,
    readonly modifiedonbehalfbyyominame: string,
    readonly owningbusinessunitname: string,
    statecode: dft_meeting_statecode,
    statuscode: dft_meeting_statuscode,
}>

export type dft_meetingfeatureoverride = TableRow<{
    // Primary Key Column
    readonly dft_meetingfeatureoverrideid: string,
    readonly createdbyname: string,
    readonly createdbyyominame: string,
    readonly createdonbehalfbyname: string,
    readonly createdonbehalfbyyominame: string,
    // Foreign Key Column
    readonly _dft_featuredefinitionid_value: `/dft_featuredefinition(${string})`,
    readonly dft_featuredefinitionidname: string,
    // Foreign Key Column
    _dft_meetingid_value: `/dft_meeting(${string})`,
    readonly dft_meetingidname: string,
    dft_name: string,
    dft_reason: string,
    dft_statecode: dft_meetingfeatureoverride_dft_statecode,
    readonly modifiedbyname: string,
    readonly modifiedbyyominame: string,
    readonly modifiedonbehalfbyname: string,
    readonly modifiedonbehalfbyyominame: string,
    readonly owningbusinessunitname: string,
    statecode: dft_meetingfeatureoverride_statecode,
    statuscode: dft_meetingfeatureoverride_statuscode,
}>

export type dft_meetingseries = TableRow<{
    // Primary Key Column
    readonly dft_meetingseriesid: string,
    readonly createdbyname: string,
    readonly createdbyyominame: string,
    readonly createdonbehalfbyname: string,
    readonly createdonbehalfbyyominame: string,
    dft_defaultchannelid: string,
    dft_defaultlocation: string,
    // Foreign Key Column
    _dft_governancebodyid_value: `/dft_governancebody(${string})`,
    readonly dft_governancebodyidname: string,
    dft_meetingcadencecode: dft_meetingseries_dft_meetingcadencecode,
    dft_name: string,
    dft_recurrencerule: string,
    dft_seriescode: string,
    // Foreign Key Column
    readonly _dft_seriesownerid_value: `/dft_orgperson(${string})`,
    readonly dft_seriesowneridname: string,
    dft_seriesstatuscode: dft_meetingseries_dft_seriesstatuscode,
    readonly modifiedbyname: string,
    readonly modifiedbyyominame: string,
    readonly modifiedonbehalfbyname: string,
    readonly modifiedonbehalfbyyominame: string,
    readonly owningbusinessunitname: string,
    statecode: dft_meetingseries_statecode,
    statuscode: dft_meetingseries_statuscode,
}>

export type dft_minutes = TableRow<{
    // Primary Key Column
    readonly dft_minutesid: string,
    readonly createdbyname: string,
    readonly createdbyyominame: string,
    readonly createdonbehalfbyname: string,
    readonly createdonbehalfbyyominame: string,
    // Foreign Key Column
    readonly _dft_approvedbyid_value: `/dft_orgperson(${string})`,
    readonly dft_approvedbyidname: string,
    dft_approvedon: Date,
    dft_classificationcode: dft_minutes_dft_classificationcode,
    dft_drafttext: string,
    // Foreign Key Column
    _dft_meetingid_value: `/dft_meeting(${string})`,
    readonly dft_meetingidname: string,
    dft_name: string,
    dft_publishedon: Date,
    dft_publishedurl: string,
    dft_statuscode: dft_minutes_dft_statuscode,
    dft_versionnumber: number,
    readonly modifiedbyname: string,
    readonly modifiedbyyominame: string,
    readonly modifiedonbehalfbyname: string,
    readonly modifiedonbehalfbyyominame: string,
    readonly owningbusinessunitname: string,
    statecode: dft_minutes_statecode,
    statuscode: dft_minutes_statuscode,
}>

export type dft_paper = TableRow<{
    // Primary Key Column
    readonly dft_paperid: string,
    readonly createdbyname: string,
    readonly createdbyyominame: string,
    readonly createdonbehalfbyname: string,
    readonly createdonbehalfbyyominame: string,
    // Foreign Key Column
    _dft_agendaitemid_value: `/dft_agendaitem(${string})`,
    readonly dft_agendaitemidname: string,
    dft_classificationcode: dft_paper_dft_classificationcode,
    dft_documentid: string,
    dft_documentsourcecode: dft_paper_dft_documentsourcecode,
    // Foreign Key Column
    _dft_meetingid_value: `/dft_meeting(${string})`,
    readonly dft_meetingidname: string,
    dft_name: string,
    // Foreign Key Column
    readonly _dft_ownerid_value: `/dft_orgperson(${string})`,
    readonly dft_owneridname: string,
    dft_paperstatuscode: dft_paper_dft_paperstatuscode,
    dft_sharepointurl: string,
    readonly modifiedbyname: string,
    readonly modifiedbyyominame: string,
    readonly modifiedonbehalfbyname: string,
    readonly modifiedonbehalfbyyominame: string,
    readonly owningbusinessunitname: string,
    statecode: dft_paper_statecode,
    statuscode: dft_paper_statuscode,
}>

export type dft_termsofreference = TableRow<{
    // Primary Key Column
    readonly dft_termsofreferenceid: string,
    readonly createdbyname: string,
    readonly createdbyyominame: string,
    readonly createdonbehalfbyname: string,
    readonly createdonbehalfbyyominame: string,
    dft_approvalstatuscode: dft_termsofreference_dft_approvalstatuscode,
    dft_approvedon: Date,
    dft_effectivefrom: Date,
    dft_effectiveto: Date,
    // Foreign Key Column
    _dft_governancebodyid_value: `/dft_governancebody(${string})`,
    readonly dft_governancebodyidname: string,
    dft_name: string,
    dft_quorumdenominatorcode: dft_termsofreference_dft_quorumdenominatorcode,
    dft_quorumnumber: number,
    dft_reviewdueon: Date,
    dft_statuscode: dft_termsofreference_dft_statuscode,
    dft_versionnumber: number,
    readonly modifiedbyname: string,
    readonly modifiedbyyominame: string,
    readonly modifiedonbehalfbyname: string,
    readonly modifiedonbehalfbyyominame: string,
    readonly owningbusinessunitname: string,
    statecode: dft_termsofreference_statecode,
    statuscode: dft_termsofreference_statuscode,
}>

const enum dft_actionitem_dft_prioritycode {
"Low" = 602170000,
"Normal" = 602170001,
"High" = 602170002,
"Urgent" = 602170003,
}
const enum dft_actionitem_dft_statuscode {
"Open" = 602170000,
"In Progress" = 602170001,
"Blocked" = 602170002,
"Completed" = 602170003,
"Overdue" = 602170004,
"Closed" = 602170005,
"Cancelled" = 602170006,
}
const enum dft_actionitem_statecode {
"Active" = 0,
"Inactive" = 1,
}
const enum dft_actionitem_statuscode {
"Active" = 1,
"Inactive" = 2,
}
const enum dft_agendaitem_dft_decisionsought {
"False" = 0,
"True" = 1,
}
const enum dft_agendaitem_dft_purposecode {
"Information" = 602170000,
"Discussion" = 602170001,
"Decision" = 602170002,
"Action" = 602170003,
"Ratification" = 602170004,
"Noting" = 602170005,
"Other" = 602170006,
}
const enum dft_agendaitem_dft_statuscode {
"Planned" = 602170000,
"Ready" = 602170001,
"Held" = 602170002,
"Deferred" = 602170003,
"Withdrawn" = 602170004,
}
const enum dft_agendaitem_statecode {
"Active" = 0,
"Inactive" = 1,
}
const enum dft_agendaitem_statuscode {
"Active" = 1,
"Inactive" = 2,
}
const enum dft_committeemembership_dft_isalternate {
"False" = 0,
"True" = 1,
}
const enum dft_committeemembership_dft_membershiprolecode {
"Chair" = 602170000,
"Deputy Chair" = 602170001,
"Secretary" = 602170002,
"Member" = 602170003,
"Alternate" = 602170004,
"Observer" = 602170005,
"Guest" = 602170006,
"Presenter" = 602170007,
}
const enum dft_committeemembership_dft_membershipstatuscode {
"Pending" = 602170000,
"Active" = 602170001,
"Suspended" = 602170002,
"Ended" = 602170003,
}
const enum dft_committeemembership_dft_votingrights {
"False" = 0,
"True" = 1,
}
const enum dft_committeemembership_statecode {
"Active" = 0,
"Inactive" = 1,
}
const enum dft_committeemembership_statuscode {
"Active" = 1,
"Inactive" = 2,
}
const enum dft_decision_dft_statuscode {
"Proposed" = 602170000,
"Resolved" = 602170001,
"Deferred" = 602170002,
"Rescinded" = 602170003,
"Superseded" = 602170004,
}
const enum dft_decision_statecode {
"Active" = 0,
"Inactive" = 1,
}
const enum dft_decision_statuscode {
"Active" = 1,
"Inactive" = 2,
}
const enum dft_governancebody_dft_bodytypecode {
"Board" = 602170000,
"Council" = 602170001,
"Committee" = 602170002,
"Subcommittee" = 602170003,
"Working Group" = 602170004,
"Project Board" = 602170005,
"Executive Forum" = 602170006,
"Advisory Panel" = 602170007,
"Task Force" = 602170008,
"Other" = 602170009,
}
const enum dft_governancebody_dft_defaultclassificationcode {
"Public" = 602170000,
"Internal" = 602170001,
"Confidential" = 602170002,
"Restricted" = 602170003,
"Legal" = 602170004,
}
const enum dft_governancebody_dft_statuscode {
"Draft" = 602170000,
"Active" = 602170001,
"Suspended" = 602170002,
"Retired" = 602170003,
}
const enum dft_governancebody_statecode {
"Active" = 0,
"Inactive" = 1,
}
const enum dft_governancebody_statuscode {
"Active" = 1,
"Inactive" = 2,
}
const enum dft_meeting_dft_classificationcode {
"Public" = 602170000,
"Internal" = 602170001,
"Confidential" = 602170002,
"Restricted" = 602170003,
"Legal" = 602170004,
}
const enum dft_meeting_dft_meetingstatuscode {
"Draft" = 602170000,
"Planned" = 602170001,
"Invited" = 602170002,
"In Progress" = 602170003,
"Minutes Drafting" = 602170004,
"Ready For Review" = 602170005,
"Approved" = 602170006,
"Published" = 602170007,
"Cancelled" = 602170008,
"Deferred" = 602170009,
}
const enum dft_meeting_statecode {
"Active" = 0,
"Inactive" = 1,
}
const enum dft_meeting_statuscode {
"Active" = 1,
"Inactive" = 2,
}
const enum dft_meetingfeatureoverride_dft_statecode {
"Enabled" = 602170000,
"Disabled" = 602170001,
}
const enum dft_meetingfeatureoverride_statecode {
"Active" = 0,
"Inactive" = 1,
}
const enum dft_meetingfeatureoverride_statuscode {
"Active" = 1,
"Inactive" = 2,
}
const enum dft_meetingseries_dft_meetingcadencecode {
"One Off" = 602170000,
"Weekly" = 602170001,
"Fortnightly" = 602170002,
"Monthly" = 602170003,
"Quarterly" = 602170004,
"Custom" = 602170005,
}
const enum dft_meetingseries_dft_seriesstatuscode {
"Draft" = 602170000,
"Active" = 602170001,
"Paused" = 602170002,
"Retired" = 602170003,
}
const enum dft_meetingseries_statecode {
"Active" = 0,
"Inactive" = 1,
}
const enum dft_meetingseries_statuscode {
"Active" = 1,
"Inactive" = 2,
}
const enum dft_minutes_dft_classificationcode {
"Public" = 602170000,
"Internal" = 602170001,
"Confidential" = 602170002,
"Restricted" = 602170003,
"Legal" = 602170004,
}
const enum dft_minutes_dft_statuscode {
"Draft" = 602170000,
"Chair Review" = 602170001,
"Approved" = 602170002,
"Published" = 602170003,
"Archived" = 602170004,
}
const enum dft_minutes_statecode {
"Active" = 0,
"Inactive" = 1,
}
const enum dft_minutes_statuscode {
"Active" = 1,
"Inactive" = 2,
}
const enum dft_paper_dft_classificationcode {
"Public" = 602170000,
"Internal" = 602170001,
"Confidential" = 602170002,
"Restricted" = 602170003,
"Legal" = 602170004,
}
const enum dft_paper_dft_documentsourcecode {
"SharePoint" = 602170000,
"Teams" = 602170001,
"Email" = 602170002,
"Uploaded" = 602170003,
"Dataverse File" = 602170004,
"External Link" = 602170005,
}
const enum dft_paper_dft_paperstatuscode {
"Draft" = 602170000,
"Submitted" = 602170001,
"Circulated" = 602170002,
"Superseded" = 602170003,
"Withdrawn" = 602170004,
"Archived" = 602170005,
}
const enum dft_paper_statecode {
"Active" = 0,
"Inactive" = 1,
}
const enum dft_paper_statuscode {
"Active" = 1,
"Inactive" = 2,
}
const enum dft_termsofreference_dft_approvalstatuscode {
"Pending" = 602170000,
"Approved" = 602170001,
"Rejected" = 602170002,
"Withdrawn" = 602170003,
}
const enum dft_termsofreference_dft_quorumdenominatorcode {
"Total Members" = 602170000,
"Voting Members" = 602170001,
"Appointed Members" = 602170002,
"Eligible Attendees" = 602170003,
}
const enum dft_termsofreference_dft_statuscode {
"Draft" = 602170000,
"Current" = 602170001,
"Superseded" = 602170002,
"Archived" = 602170003,
}
const enum dft_termsofreference_statecode {
"Active" = 0,
"Inactive" = 1,
}
const enum dft_termsofreference_statuscode {
"Active" = 1,
"Inactive" = 2,
}

export interface UxAgentDataApi extends BaseUxAgentDataApi<TableRegistrations, EnumRegistrations> {}

export interface GeneratedComponentProps {
    dataApi: UxAgentDataApi;
}
