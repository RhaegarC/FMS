export interface SchemaProperty {
  type: string;
  title?: string;
  description?: string;
  enum?: string[];
  format?: string;
  minimum?: number;
  maximum?: number;
  "x-dataSource"?: { url: string; valueField: string; labelField: string };
}

export interface SectionField {
  name: string;
  column: number; // 1-based
}

export interface SchemaSection {
  id: string;
  title: string;
  columns: number; // 1–4
  fields: SectionField[];
}

export interface JsonSchema {
  title?: string;
  description?: string;
  type: string;
  properties: Record<string, SchemaProperty>;
  required?: string[];
  if?: { properties?: Record<string, { const?: unknown; enum?: unknown[] }> };
  then?: { required?: string[] };
  else?: { required?: string[] };
  "x-sections"?: SchemaSection[];
}

export interface Space {
  id: string;
  name: string;
  description?: string;
  color: string;
}

export interface Form {
  id: string;
  spaceId: string;
  name: string;
  schema: JsonSchema;
  updatedAt: string;
  submissionCount: number;
}

export interface AppUser {
  id: string;
  name: string;
  email: string;
  role: "admin" | "user";
  initials: string;
}

export interface Submission {
  id: string;
  formId: string;
  formName: string;
  spaceId: string;
  spaceName: string;
  userId: string;
  userName: string;
  userEmail: string;
  data: Record<string, unknown>;
  createdAt: string;
}

export const spaces: Space[] = [
  { id: "hr", name: "Human Resources", color: "#8b5cf6" },
  { id: "finance", name: "Finance", color: "#f59e0b" },
  { id: "ops", name: "Operations", color: "#3d7fff" },
];

export const forms: Form[] = [
  {
    id: "onboarding",
    spaceId: "hr",
    name: "Employee Onboarding",
    updatedAt: "2026-08-20",
    submissionCount: 47,
    schema: {
      title: "Employee Onboarding",
      description: "Complete this form to initiate the onboarding process for a new hire.",
      type: "object",
      properties: {
        firstName: { type: "string", title: "First Name" },
        lastName: { type: "string", title: "Last Name" },
        workEmail: { type: "string", title: "Work Email", format: "email" },
        department: {
          type: "string",
          title: "Department",
          enum: ["Engineering", "Marketing", "Sales", "Human Resources", "Finance", "Operations"],
        },
        startDate: { type: "string", format: "date", title: "Start Date" },
        employmentType: {
          type: "string",
          title: "Employment Type",
          enum: ["Full-time", "Part-time", "Contract"],
        },
        remote: { type: "boolean", title: "Remote Employee" },
        notes: {
          type: "string",
          format: "textarea",
          title: "Additional Notes",
          description: "Required for remote employees — include timezone and equipment needs.",
        },
      },
      required: ["firstName", "lastName", "workEmail", "department", "startDate"],
      if: { properties: { remote: { const: true } } },
      then: { required: ["firstName", "lastName", "workEmail", "department", "startDate", "notes"] },
      "x-sections": [
        {
          id: "s-personal",
          title: "Personal Information",
          columns: 2,
          fields: [
            { name: "firstName", column: 1 },
            { name: "lastName", column: 2 },
            { name: "workEmail", column: 1 },
          ],
        },
        {
          id: "s-employment",
          title: "Employment Details",
          columns: 3,
          fields: [
            { name: "department", column: 1 },
            { name: "startDate", column: 2 },
            { name: "employmentType", column: 3 },
          ],
        },
        {
          id: "s-setup",
          title: "Work Setup",
          columns: 1,
          fields: [
            { name: "remote", column: 1 },
            { name: "notes", column: 1 },
          ],
        },
      ],
    },
  },
  {
    id: "exit",
    spaceId: "hr",
    name: "Exit Interview",
    updatedAt: "2026-07-15",
    submissionCount: 12,
    schema: {
      title: "Exit Interview",
      type: "object",
      properties: {
        employeeName: { type: "string", title: "Employee Name" },
        lastDay: { type: "string", format: "date", title: "Last Working Day" },
        reason: {
          type: "string",
          title: "Reason for Leaving",
          enum: ["Better Opportunity", "Compensation", "Relocation", "Personal", "Retirement", "Other"],
        },
        rating: { type: "integer", title: "Overall Experience (1–10)" },
        feedback: { type: "string", format: "textarea", title: "What could we have done better?" },
        wouldRecommend: { type: "boolean", title: "Would you recommend us as an employer?" },
      },
      required: ["employeeName", "lastDay", "reason"],
    },
  },
  {
    id: "expense",
    spaceId: "finance",
    name: "Expense Report",
    updatedAt: "2026-08-18",
    submissionCount: 134,
    schema: {
      title: "Expense Report",
      type: "object",
      properties: {
        submitterName: { type: "string", title: "Submitter" },
        amount: { type: "number", title: "Total Amount (USD)" },
        category: {
          type: "string",
          title: "Category",
          enum: ["Travel", "Meals & Entertainment", "Software", "Hardware", "Training", "Office Supplies", "Other"],
        },
        expenseDate: { type: "string", format: "date", title: "Expense Date" },
        projectCode: { type: "string", title: "Project Code" },
        description: { type: "string", format: "textarea", title: "Description & Justification" },
        reimbursable: { type: "boolean", title: "Reimbursable" },
        receiptAttached: { type: "boolean", title: "Receipt Attached" },
      },
      required: ["submitterName", "amount", "category", "expenseDate", "description"],
      "x-sections": [
        {
          id: "s-report",
          title: "Report Details",
          columns: 2,
          fields: [
            { name: "submitterName", column: 1 },
            { name: "amount", column: 2 },
            { name: "category", column: 1 },
            { name: "expenseDate", column: 2 },
            { name: "projectCode", column: 1 },
          ],
        },
        {
          id: "s-desc",
          title: "Description & Flags",
          columns: 1,
          fields: [
            { name: "description", column: 1 },
            { name: "reimbursable", column: 1 },
            { name: "receiptAttached", column: 1 },
          ],
        },
      ],
    },
  },
  {
    id: "budget",
    spaceId: "finance",
    name: "Budget Request",
    updatedAt: "2026-08-01",
    submissionCount: 23,
    schema: {
      title: "Budget Request",
      type: "object",
      properties: {
        requestorName: { type: "string", title: "Requestor Name" },
        department: {
          type: "string",
          title: "Department",
          enum: ["Engineering", "Marketing", "Sales", "HR", "Finance", "Operations"],
        },
        amount: { type: "number", title: "Requested Amount (USD)" },
        quarter: {
          type: "string",
          title: "Fiscal Quarter",
          enum: ["Q1 2027", "Q2 2027", "Q3 2027", "Q4 2027"],
        },
        justification: { type: "string", format: "textarea", title: "Business Justification" },
        urgent: { type: "boolean", title: "Mark as Urgent" },
      },
      required: ["requestorName", "department", "amount", "quarter", "justification"],
    },
  },
  {
    id: "incident",
    spaceId: "ops",
    name: "Incident Report",
    updatedAt: "2026-08-10",
    submissionCount: 8,
    schema: {
      title: "Incident Report",
      type: "object",
      properties: {
        title: { type: "string", title: "Incident Title" },
        severity: {
          type: "string",
          title: "Severity",
          enum: ["Low", "Medium", "High", "Critical"],
        },
        incidentDate: { type: "string", format: "date", title: "Date of Incident" },
        affectedSystems: { type: "string", title: "Affected Systems" },
        description: { type: "string", format: "textarea", title: "Description" },
        rootCause: { type: "string", format: "textarea", title: "Root Cause Analysis" },
        resolved: { type: "boolean", title: "Resolved" },
      },
      required: ["title", "severity", "incidentDate", "description"],
    },
  },
];

export const adminUser: AppUser = {
  id: "u-1",
  name: "Jordan Park",
  email: "jordan.park@company.com",
  role: "admin",
  initials: "JP",
};

export const normalUser: AppUser = {
  id: "u-4",
  name: "Alex Okafor",
  email: "alex.okafor@company.com",
  role: "user",
  initials: "AO",
};

export const submissions: Submission[] = [
  {
    id: "1042",
    formId: "onboarding",
    formName: "Employee Onboarding",
    spaceId: "hr",
    spaceName: "Human Resources",
    userId: "u-3",
    userName: "Sarah Chen",
    userEmail: "sarah.chen@company.com",
    createdAt: "2026-08-29T10:23:00Z",
    data: {
      firstName: "Sarah",
      lastName: "Chen",
      workEmail: "s.chen@company.com",
      department: "Engineering",
      startDate: "2026-09-01",
      employmentType: "Full-time",
      remote: true,
      notes: "Based in Seattle, WA. Needs standing desk, 27″ monitor, and external keyboard.",
    },
  },
  {
    id: "1041",
    formId: "expense",
    formName: "Expense Report",
    spaceId: "finance",
    spaceName: "Finance",
    userId: "u-5",
    userName: "Marco Ricci",
    userEmail: "marco.ricci@company.com",
    createdAt: "2026-08-28T14:05:00Z",
    data: {
      submitterName: "Marco Ricci",
      amount: 342.5,
      category: "Travel",
      expenseDate: "2026-08-25",
      projectCode: "PROJ-2041",
      description: "Round-trip flight and 1-night hotel for client meeting in Chicago.",
      reimbursable: true,
      receiptAttached: true,
    },
  },
  {
    id: "1040",
    formId: "incident",
    formName: "Incident Report",
    spaceId: "ops",
    spaceName: "Operations",
    userId: "u-6",
    userName: "Priya Kumar",
    userEmail: "priya.kumar@company.com",
    createdAt: "2026-08-27T09:15:00Z",
    data: {
      title: "API Gateway Timeout During Peak Hours",
      severity: "High",
      incidentDate: "2026-08-26",
      affectedSystems: "API Gateway, User Portal, Auth Service",
      description: "The API gateway experienced repeated timeouts between 09:00–11:30 UTC affecting ~35% of user requests.",
      rootCause: "Connection pool exhaustion caused by an unoptimised query introduced in deploy v2.14.3.",
      resolved: true,
    },
  },
  {
    id: "1039",
    formId: "budget",
    formName: "Budget Request",
    spaceId: "finance",
    spaceName: "Finance",
    userId: "u-2",
    userName: "David Kim",
    userEmail: "david.kim@company.com",
    createdAt: "2026-08-26T16:45:00Z",
    data: {
      requestorName: "David Kim",
      department: "Engineering",
      amount: 45000,
      quarter: "Q1 2027",
      justification: "Additional cloud infrastructure for the new microservices rollout — compute, storage, and CDN costs.",
      urgent: false,
    },
  },
  {
    id: "1038",
    formId: "exit",
    formName: "Exit Interview",
    spaceId: "hr",
    spaceName: "Human Resources",
    userId: "u-7",
    userName: "Lena Fischer",
    userEmail: "lena.fischer@company.com",
    createdAt: "2026-08-25T11:30:00Z",
    data: {
      employeeName: "Lena Fischer",
      lastDay: "2026-08-31",
      reason: "Better Opportunity",
      rating: 8,
      feedback: "Great team culture. Better tooling and structured mentorship programs would have been strong retention factors.",
      wouldRecommend: true,
    },
  },
  {
    id: "1037",
    formId: "expense",
    formName: "Expense Report",
    spaceId: "finance",
    spaceName: "Finance",
    userId: "u-4",
    userName: "Alex Okafor",
    userEmail: "alex.okafor@company.com",
    createdAt: "2026-08-24T08:22:00Z",
    data: {
      submitterName: "Alex Okafor",
      amount: 89,
      category: "Software",
      expenseDate: "2026-08-20",
      projectCode: "PROJ-1892",
      description: "Annual Figma Professional licence renewal.",
      reimbursable: true,
      receiptAttached: true,
    },
  },
  {
    id: "1036",
    formId: "onboarding",
    formName: "Employee Onboarding",
    spaceId: "hr",
    spaceName: "Human Resources",
    userId: "u-4",
    userName: "Alex Okafor",
    userEmail: "alex.okafor@company.com",
    createdAt: "2026-07-10T13:00:00Z",
    data: {
      firstName: "Alex",
      lastName: "Okafor",
      workEmail: "alex.okafor@company.com",
      department: "Marketing",
      startDate: "2026-07-15",
      employmentType: "Full-time",
      remote: false,
      notes: "",
    },
  },
];

export const userSubmissions = submissions.filter((s) => s.userId === "u-4");

export const userAccessibleForms = forms.filter((f) =>
  ["onboarding", "expense", "budget"].includes(f.id),
);
