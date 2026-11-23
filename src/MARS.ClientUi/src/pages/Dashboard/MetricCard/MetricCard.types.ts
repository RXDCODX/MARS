export interface MetricCardProps {
  title: string;
  value: string;
  icon: string;
  color: "blue" | "green" | "purple" | "orange" | "red" | "teal";
  trend: string;
  trendDirection: "up" | "down" | "stable";
}
