function ProcesarRespuesta(data) {
    if (data.salidas && Array.isArray(data.salidas) && data.salidas.length > 0) {
        data.salidas.forEach(item => EnviarMensaje(item.codigo, item.mensaje));
    } 
    else if (data.mensaje) {
        EnviarMensaje(data.codigo || 0, data.mensaje);
    }
    else {
        console.warn("La respuesta no contiene mensajes:", data);
    }
}
