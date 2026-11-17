// Textos legales de ejemplo (proyecto académico). Cada sección tiene un id estable para el
// índice y los enlaces directos (/terminos#reservas).

export interface LegalSection {
  id: string;
  title: string;
  paragraphs: string[];
}

export interface LegalDoc {
  title: string;
  updated: string; // ISO
  intro: string;
  sections: LegalSection[];
}

export type LegalDocId = "terminos" | "privacidad";

export const LEGAL: Record<LegalDocId, LegalDoc> = {
  terminos: {
    title: "Términos y condiciones",
    updated: "2026-11-17",
    intro:
      "Estas condiciones regulan el uso de Wheelby, un marketplace donde particulares alquilan vehículos a otros particulares.",
    sections: [
      {
        id: "aceptacion",
        title: "Aceptación",
        paragraphs: [
          "Al crear una cuenta o usar Wheelby aceptas estos términos. Si no estás de acuerdo con alguno, no uses el servicio.",
          "Debes ser mayor de edad y tener capacidad legal para contratar en la República Dominicana.",
        ],
      },
      {
        id: "cuentas",
        title: "Cuentas y verificación de correo",
        paragraphs: [
          "Tu cuenta es personal. Para usarla debes activarla desde el enlace que enviamos a tu correo.",
          "Eres responsable de mantener tu contraseña en secreto y de la actividad que ocurra con tu cuenta. Si sospechas un acceso no autorizado, cambia tu contraseña y avísanos.",
        ],
      },
      {
        id: "roles",
        title: "Anfitriones y arrendatarios",
        paragraphs: [
          "Cualquier usuario puede reservar vehículos como arrendatario. Para publicar vehículos necesitas ser anfitrión verificado: lo solicitas desde tu perfil y un administrador revisa la solicitud.",
          "El anfitrión declara que el vehículo que publica le pertenece o que tiene derecho a alquilarlo, y que los datos que publica son correctos.",
        ],
      },
      {
        id: "reservas",
        title: "Reservas y cancelaciones",
        paragraphs: [
          "Una reserva empieza como solicitud. El anfitrión la confirma o la rechaza; si llega la fecha de inicio sin respuesta, la solicitud expira.",
          "El arrendatario y el anfitrión pueden cancelar antes de la fecha de inicio. El anfitrión debe indicar un motivo. Un alquiler en curso no se cancela: se completa.",
          "El precio por día queda fijado al solicitar. Wheelby no cobra pagos en línea; las partes acuerdan el pago.",
        ],
      },
      {
        id: "documentos",
        title: "Documentos",
        paragraphs: [
          "Puedes subir documentos como tu licencia o la matrícula del vehículo, y adjuntar actas de entrega y devolución a una reserva.",
          "Solo subas archivos propios o que tengas derecho a compartir. Los documentos adjuntos a una reserva los ven las dos partes.",
        ],
      },
      {
        id: "conducta",
        title: "Conducta",
        paragraphs: [
          "No uses Wheelby para fines ilegales, para suplantar a otra persona ni para publicar información falsa.",
          "Trata a la otra parte con respeto y cuida el vehículo como si fuera tuyo. Podemos suspender cuentas que incumplan estas normas.",
        ],
      },
      {
        id: "responsabilidad",
        title: "Limitación de responsabilidad",
        paragraphs: [
          "Wheelby conecta a anfitriones y arrendatarios, pero no es parte del contrato de alquiler entre ellos.",
          "No respondemos por daños, accidentes o disputas entre usuarios, en la medida en que la ley lo permita.",
        ],
      },
      {
        id: "cambios",
        title: "Cambios",
        paragraphs: [
          "Podemos actualizar estos términos. Publicaremos la nueva versión con su fecha y, si el cambio es importante, te avisaremos por correo.",
        ],
      },
    ],
  },
  privacidad: {
    title: "Política de privacidad",
    updated: "2026-11-17",
    intro:
      "Esta política explica qué datos personales recopila Wheelby, para qué los usa y qué derechos tienes.",
    sections: [
      {
        id: "datos",
        title: "Datos que recopilamos",
        paragraphs: [
          "Datos de tu cuenta: nombre, correo y contraseña (guardada como hash, nunca en texto).",
          "Datos de uso: vehículos que publicas, reservas que haces o recibes, reseñas y documentos que subes.",
        ],
      },
      {
        id: "uso",
        title: "Cómo los usamos",
        paragraphs: [
          "Para operar el servicio: identificarte, mostrar tus vehículos y reservas, y enviarte avisos por correo cuando una reserva cambia de estado.",
          "No vendemos tus datos ni los usamos para publicidad de terceros.",
        ],
      },
      {
        id: "documentos",
        title: "Documentos (acceso restringido)",
        paragraphs: [
          "Los documentos de tu perfil solo los ves tú y el equipo de Wheelby.",
          "Los documentos adjuntos a una reserva los ven el arrendatario, el anfitrión y el equipo de Wheelby. Las fotos de los vehículos las ve cualquier usuario con sesión.",
        ],
      },
      {
        id: "conservacion",
        title: "Conservación",
        paragraphs: [
          "Conservamos tus datos mientras tu cuenta esté activa y el tiempo que exija la ley después.",
          "Un documento eliminado deja de estar disponible de inmediato.",
        ],
      },
      {
        id: "derechos",
        title: "Tus derechos",
        paragraphs: [
          "Puedes acceder a tus datos, corregirlos y pedir que los eliminemos. También puedes oponerte a ciertos usos.",
        ],
      },
      {
        id: "contacto",
        title: "Contacto",
        paragraphs: [
          "Para cualquier consulta sobre privacidad, escríbenos a privacidad@wheelby.example.",
        ],
      },
    ],
  },
};
