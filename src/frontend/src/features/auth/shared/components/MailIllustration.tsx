export default function MailIllustration({
  className,
}: {
  className?: string;
}) {
  return (
    <svg className={className} viewBox="0 0 200 150" aria-hidden="true">
      <ellipse cx="100" cy="136" rx="70" ry="8" fill="#F0F0F0" />
      <circle cx="100" cy="70" r="58" fill="var(--primary-soft)" />
      <g transform="translate(46 44)">
        <rect
          x="0"
          y="10"
          width="108"
          height="72"
          rx="12"
          fill="#fff"
          stroke="var(--primary)"
          strokeWidth="2.5"
        />
        <path
          d="M4 16 54 52 104 16"
          fill="none"
          stroke="var(--primary)"
          strokeWidth="2.5"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
        <path
          d="M4 78 40 46M104 78 68 46"
          fill="none"
          stroke="var(--primary)"
          strokeWidth="2"
          strokeLinecap="round"
          opacity=".35"
        />
      </g>
      <circle cx="148" cy="46" r="17" fill="var(--highlight)" />
      <path
        d="m140 46 6 6 10-11"
        fill="none"
        stroke="#fff"
        strokeWidth="3"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path
        d="M40 34c.4 3 1.7 4.3 4.6 4.6-2.9.4-4.2 1.7-4.6 4.6-.4-2.9-1.7-4.2-4.6-4.6 2.9-.3 4.2-1.6 4.6-4.6Z"
        fill="var(--highlight)"
      />
      <path
        d="M164 92c.3 2.2 1.2 3.1 3.4 3.4-2.2.3-3.1 1.2-3.4 3.4-.3-2.2-1.2-3.1-3.4-3.4 2.2-.3 3.1-1.2 3.4-3.4Z"
        fill="var(--primary)"
        opacity=".5"
      />
    </svg>
  );
}
