import SectionHeading from "@/components/custom/SectionHeading";
import { isAdmin, useSession } from "@/stores/session.store";
import DocumentsManager from "../components/DocumentsManager";

// Documentos de la cuenta. El Administrador ve los de todos.
export default function DocumentsSection() {
  const user = useSession((s) => s.user);
  return (
    <DocumentsManager
      heading={(upload) => (
        <SectionHeading
          title="Documentos"
          description={
            isAdmin(user)
              ? "Los archivos de todas las cuentas. Hasta 10 MB por archivo."
              : "Tus archivos guardados en Wheelby. Hasta 10 MB por archivo."
          }
        >
          {upload}
        </SectionHeading>
      )}
    />
  );
}
