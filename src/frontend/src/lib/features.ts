// Funciones que dependen de un ajuste del backend (etiquetas P1 a P4 del diseño). Cada pantalla
// pregunta aquí antes de usar el endpoint, así que apagar una no obliga a tocar el resto.
// Todas están activas porque la API de esta rama ya las ofrece.
export const features = {
  // P1: GET /vehicles/{id}. Sin él, la ficha solo se abre desde un resultado de búsqueda.
  vehicleDetail: true,
  // P1: GET /vehicles/{id}/availability. Sin él, el calendario no marca fechas ocupadas.
  availability: true,
  // P1: GET /reservations/{id}. Sin él, tras reservar se vuelve a "Mis reservas".
  reservationDetail: true,
  // P3: GET /reservations/{id}/history (línea de tiempo con quién actuó y el motivo).
  reservationHistory: true,
  // P3: documentos de entrega y devolución adjuntos a la reserva.
  reservationDocuments: true,
  // P4: reseñas de reservas completadas y sección de reseñas en la ficha.
  reviews: true,
} as const;
