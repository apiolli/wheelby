export const DemoCode = ({ children }: { children: string }) => {
  return (
    <code className="rounded bg-secondary px-1.5 py-px text-[11.5px]">
      {children}
    </code>
  );
};
