// Ilustraciones de los estados vacíos, en el estilo de MailIllustration: círculo primary-soft,
// trazo petróleo y un acento ámbar.

export function CalendarIllustration({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 160 120" aria-hidden="true">
      <ellipse cx="80" cy="110" rx="52" ry="6" fill="var(--secondary)" />
      <circle cx="80" cy="56" r="46" fill="var(--primary-soft)" />
      <rect
        x="46"
        y="30"
        width="68"
        height="58"
        rx="10"
        fill="var(--background)"
        stroke="var(--primary)"
        strokeWidth="2.5"
      />
      <path d="M46 46h68" stroke="var(--primary)" strokeWidth="2.5" />
      <path
        d="M62 24v12M98 24v12"
        stroke="var(--primary)"
        strokeWidth="2.5"
        strokeLinecap="round"
      />
      <g fill="var(--primary)" opacity=".3">
        <rect x="56" y="54" width="10" height="8" rx="2" />
        <rect x="75" y="54" width="10" height="8" rx="2" />
        <rect x="56" y="70" width="10" height="8" rx="2" />
        <rect x="75" y="70" width="10" height="8" rx="2" />
      </g>
      <rect
        x="94"
        y="54"
        width="10"
        height="8"
        rx="2"
        fill="var(--highlight)"
      />
      <path
        d="M126 28c.4 3 1.7 4.3 4.6 4.6-2.9.4-4.2 1.7-4.6 4.6-.4-2.9-1.7-4.2-4.6-4.6 2.9-.3 4.2-1.6 4.6-4.6Z"
        fill="var(--highlight)"
      />
    </svg>
  );
}

export function CarIllustration({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 160 120" aria-hidden="true">
      <ellipse cx="80" cy="110" rx="52" ry="6" fill="var(--secondary)" />
      <circle cx="80" cy="56" r="46" fill="var(--primary-soft)" />
      <path
        d="M44 76V64l9-17a7 7 0 0 1 6-4h42a7 7 0 0 1 6 4l9 17v12Z"
        fill="var(--background)"
        stroke="var(--primary)"
        strokeWidth="2.5"
        strokeLinejoin="round"
      />
      <path d="M44 64h72" stroke="var(--primary)" strokeWidth="2.5" />
      <circle
        cx="60"
        cy="78"
        r="8"
        fill="var(--background)"
        stroke="var(--primary)"
        strokeWidth="2.5"
      />
      <circle
        cx="100"
        cy="78"
        r="8"
        fill="var(--background)"
        stroke="var(--primary)"
        strokeWidth="2.5"
      />
      <circle cx="116" cy="36" r="10" fill="var(--highlight)" />
      <path
        d="M111.5 36h9"
        stroke="var(--background)"
        strokeWidth="2.6"
        strokeLinecap="round"
      />
    </svg>
  );
}

export function DocumentIllustration({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 160 120" aria-hidden="true">
      <ellipse cx="80" cy="110" rx="52" ry="6" fill="var(--secondary)" />
      <circle cx="80" cy="56" r="46" fill="var(--primary-soft)" />
      <path
        d="M58 22h32l16 16v52a6 6 0 0 1-6 6H58a6 6 0 0 1-6-6V28a6 6 0 0 1 6-6Z"
        fill="var(--background)"
        stroke="var(--primary)"
        strokeWidth="2.5"
        strokeLinejoin="round"
      />
      <path
        d="M90 22v16h16"
        fill="none"
        stroke="var(--primary)"
        strokeWidth="2.5"
        strokeLinejoin="round"
      />
      <path
        d="M64 56h30M64 66h30M64 76h18"
        stroke="var(--primary)"
        strokeWidth="2.5"
        strokeLinecap="round"
        opacity=".35"
      />
      <circle cx="108" cy="82" r="11" fill="var(--highlight)" />
      <path
        d="M108 77.5v9M103.5 82h9"
        stroke="var(--background)"
        strokeWidth="2.6"
        strokeLinecap="round"
      />
    </svg>
  );
}

export function ReviewIllustration({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 160 120" aria-hidden="true">
      <ellipse cx="80" cy="110" rx="52" ry="6" fill="var(--secondary)" />
      <circle cx="80" cy="56" r="46" fill="var(--primary-soft)" />
      <path
        d="M46 36a10 10 0 0 1 10-10h48a10 10 0 0 1 10 10v28a10 10 0 0 1-10 10H72l-14 12v-12h-2a10 10 0 0 1-10-10Z"
        fill="var(--background)"
        stroke="var(--primary)"
        strokeWidth="2.5"
        strokeLinejoin="round"
      />
      <path
        d="M80 38l4.4 8.6 9.6 1.4-7 6.7 1.7 9.5L80 59.7l-8.7 4.5 1.7-9.5-7-6.7 9.6-1.4Z"
        fill="var(--highlight)"
      />
      <path
        d="M126 28c.4 3 1.7 4.3 4.6 4.6-2.9.4-4.2 1.7-4.6 4.6-.4-2.9-1.7-4.2-4.6-4.6 2.9-.3 4.2-1.6 4.6-4.6Z"
        fill="var(--highlight)"
      />
    </svg>
  );
}
