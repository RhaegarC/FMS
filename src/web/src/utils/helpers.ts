export function spaceColor(spaceId: string): string {
  const map: Record<string, string> = {
    hr: "bg-violet-500/10 text-violet-400 border-violet-500/20",
    finance: "bg-amber-500/10 text-amber-400 border-amber-500/20",
    ops: "bg-blue-500/10 text-blue-400 border-blue-500/20",
  };
  return map[spaceId] ?? "bg-muted text-muted-foreground border-border";
}

export function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString("en-US", {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}

export function severityColor(severity: string): string {
  const map: Record<string, string> = {
    Low: "bg-emerald-500/10 text-emerald-400",
    Medium: "bg-amber-500/10 text-amber-400",
    High: "bg-orange-500/10 text-orange-400",
    Critical: "bg-red-500/10 text-red-400",
  };
  return map[severity] ?? "bg-muted text-muted-foreground";
}
