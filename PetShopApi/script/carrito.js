// Variable global para nuestro carrito
let carrito = JSON.parse(localStorage.getItem('carrito')) || [];

// Función para añadir productos
function agregarAlCarrito(producto) {
    // 1. ¿Ya existe el producto?
    const productoExistente = carrito.find(item => item.id === producto.id);

    if (productoExistente) {
        // Solo sumamos la cantidad
        productoExistente.cantidad += 1;
    } else {
        // Lo agregamos por primera vez
        carrito.push({ ...producto, cantidad: 1 });
    }

    // 2. Guardamos en localStorage para persistencia
    localStorage.setItem('carrito', JSON.stringify(carrito));
    
    // 3. Actualizamos la vista (ej: un contador en el ícono del carrito)
    actualizarVistaCarrito();
}

// Función para eliminar un producto
function eliminarDelCarrito(productoId) {
    carrito = carrito.filter(item => item.id !== productoId);
    localStorage.setItem('carrito', JSON.stringify(carrito));
    actualizarVistaCarrito();
}

// Función para calcular el total (¡Aquí usamos el .reduce que vimos!)
function obtenerTotal() {
    return carrito.reduce((total, item) => total + (item.precio * item.cantidad), 0);
}