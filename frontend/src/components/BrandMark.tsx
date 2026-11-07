// Marca de la app: una caja de bodega sobre el amarillo de señalética.
export default function BrandMark() {
  return (
    <span className="brand-mark" aria-hidden="true">
      <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinejoin="round">
        <path d="M3 7.5 12 3l9 4.5v9L12 21l-9-4.5z" />
        <path d="M3 7.5 12 12l9-4.5M12 12v9" />
      </svg>
    </span>
  )
}
