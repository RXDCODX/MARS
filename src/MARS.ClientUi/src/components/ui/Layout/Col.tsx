interface ColProperties {
  children: React.ReactNode;
  className?: string;
  md?: number;
  sm?: number;
  lg?: number;
  xs?: number;
}

const Col = ({ children, className, md, sm, lg, xs }: ColProperties) => {
  const span = md ?? sm ?? lg ?? xs ?? 6;
  const flex = `${(span / 12) * 100}%`;

  return (
    <div className={className} style={{ flex, minWidth: 0 }}>
      {children}
    </div>
  );
};

export default Col;
