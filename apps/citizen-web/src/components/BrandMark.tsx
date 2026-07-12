// Inline logo mark inspired by the NetCAD "N": a navy-blue rounded square with a
// cyan N and a white accent square.
export function BrandMark() {
  return (
    <svg className="brand-mark" viewBox="0 0 40 40" width="40" height="40" role="img" aria-label="Belediyem logosu">
      <defs>
        <linearGradient id="brandGrad" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stopColor="#1f4bb0" />
          <stop offset="1" stopColor="#16265c" />
        </linearGradient>
      </defs>
      <rect width="40" height="40" rx="11" fill="url(#brandGrad)" />
      {/* N */}
      <path
        d="M11 29V12h3.4l8.2 10.4V12H26v17h-3.4l-8.2-10.4V29H11Z"
        fill="#22b8e8"
      />
      {/* accent square */}
      <rect x="25.5" y="11" width="6.2" height="6.2" rx="1.2" fill="#ffffff" />
    </svg>
  );
}
