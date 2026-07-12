// Inline logo mark: a location pin with a check, in the NetCAD-inspired green.
export function BrandMark() {
  return (
    <svg className="brand-mark" viewBox="0 0 40 40" width="40" height="40" role="img" aria-label="Belediyem logosu">
      <defs>
        <linearGradient id="brandGrad" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stopColor="#16b866" />
          <stop offset="1" stopColor="#00954c" />
        </linearGradient>
      </defs>
      <rect width="40" height="40" rx="11" fill="url(#brandGrad)" />
      <path
        d="M20 9c-4.4 0-8 3.5-8 7.9 0 5.4 6.7 12.1 7.4 12.8.34.33.86.33 1.2 0 .7-.7 7.4-7.4 7.4-12.8C28 12.5 24.4 9 20 9Z"
        fill="#ffffff"
      />
      <path
        d="m16.6 17.2 2.4 2.4 4.6-4.6"
        fill="none"
        stroke="#00954c"
        strokeWidth="2.2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}
