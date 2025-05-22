# Pruebas End-to-End (E2E)

Esta carpeta contiene las **pruebas end-to-end** (E2E), también conocidas como pruebas de punta a punta.

## ¿Qué son las pruebas E2E?

Las pruebas E2E simulan escenarios completos que un usuario real podría realizar en la aplicación. Estas pruebas interactúan con el sistema completo (interfaz, backend, base de datos, etc.) como si fueran un usuario utilizando el producto final.

## Objetivo

El objetivo principal de las pruebas E2E es **verificar que todas las partes del sistema funcionen juntas correctamente**. Se usan para detectar errores de integración que no se ven en pruebas unitarias o de integración aislada.

## ¿Quién las realiza?

Suelen ser realizadas por el equipo de **QA (Quality Assurance)** o por desarrolladores especializados en pruebas automatizadas.

## Ejemplos de escenarios E2E

- Un usuario inicia sesión y accede a su panel de control.
- Un usuario agrega un producto al carrito y completa una compra.
- Un usuario navega por la aplicación sin errores desde el inicio hasta el cierre de sesión.

## Herramientas comunes

- Cypress
- Playwright
- Selenium
