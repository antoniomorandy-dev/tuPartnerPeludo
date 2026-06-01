function ProcesarRespuesta(data) {
    if (data.salidas && Array.isArray(data.salidas) && data.salidas.length > 0) {
        data.salidas.forEach(item => EnviarMensaje(item.codigo, item.mensaje));
    } 
    // 2. Si no hay lista, verificamos si viene un mensaje directo (tu caso actual)
    else if (data.mensaje) {
        EnviarMensaje(data.codigo || 0, data.mensaje);
    }
    // 3. Fallback opcional por si la API falla de forma inesperada
    else {
        console.warn("La respuesta no contiene mensajes:", data);
    }
}
