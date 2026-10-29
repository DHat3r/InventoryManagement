const moneyFormat = new Intl.NumberFormat('es-CL', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
const dateTimeFormat = new Intl.DateTimeFormat('es-CL', { dateStyle: 'short', timeStyle: 'short' })

export function formatMoney(value: number): string {
  return `$ ${moneyFormat.format(value)}`
}

export function formatDateTime(isoDate: string): string {
  return dateTimeFormat.format(new Date(isoDate))
}
