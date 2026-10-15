(function () {
    "use strict";

    const clave = "NetoFitV2_Accesibilidad";
    const minimo = -2;
    const maximo = 4;
    const factores = { "-2": 0.8, "-1": 0.9, "0": 1, "1": 1.1, "2": 1.2, "3": 1.3, "4": 1.4 };
    const preferencias = { texto: 0, oscuro: false, contraste: false };

    try {
        const datos = JSON.parse(localStorage.getItem(clave) || "{}");
        if (Number.isInteger(datos.texto) && datos.texto >= minimo && datos.texto <= maximo) {
            preferencias.texto = datos.texto;
        }
        preferencias.oscuro = datos.oscuro === true;
        preferencias.contraste = datos.contraste === true;
    } catch (error) {
    }

    function guardar() {
        try {
            localStorage.setItem(clave, JSON.stringify(preferencias));
        } catch (error) {
        }
    }

    function aplicarColores() {
        document.documentElement.classList.toggle("nf-ac-oscuro", preferencias.oscuro);
        document.documentElement.classList.toggle("nf-ac-contraste", preferencias.contraste);
    }

    function aplicarTexto() {
        const elementos = document.querySelectorAll("body *");
        const factor = factores[String(preferencias.texto)];

        for (const elemento of elementos) {
            if (elemento.closest("#nf-accesibilidad, script, style, noscript, svg")) continue;

            let tieneTexto = elemento.matches("input, select, textarea");
            if (!tieneTexto) {
                for (const nodo of elemento.childNodes) {
                    if (nodo.nodeType === 3 && nodo.textContent.trim() !== "") {
                        tieneTexto = true;
                        break;
                    }
                }
            }
            if (!tieneTexto) continue;

            if (!elemento.hasAttribute("data-nf-tamano-base")) {
                elemento.setAttribute("data-nf-tamano-base", getComputedStyle(elemento).fontSize);
                elemento.setAttribute("data-nf-tamano-original", elemento.style.getPropertyValue("font-size"));
                elemento.setAttribute("data-nf-tamano-prioridad", elemento.style.getPropertyPriority("font-size"));
            }

            if (preferencias.texto === 0) {
                const original = elemento.getAttribute("data-nf-tamano-original");
                if (original) {
                    elemento.style.setProperty("font-size", original, elemento.getAttribute("data-nf-tamano-prioridad"));
                } else {
                    elemento.style.removeProperty("font-size");
                }
            } else {
                const base = parseFloat(elemento.getAttribute("data-nf-tamano-base"));
                if (Number.isFinite(base) && base > 0) {
                    elemento.style.setProperty("font-size", (base * factor).toFixed(2) + "px", "important");
                }
            }
        }
    }

    function iniciar() {
        const barra = document.getElementById("nf-accesibilidad");
        if (!barra) return;
        const estado = document.getElementById("nf-accesibilidad-estado");
        const botones = barra.querySelectorAll("[data-nf-a11y]");

        function actualizarControles() {
            for (const boton of botones) {
                const accion = boton.getAttribute("data-nf-a11y");
                if (accion === "aumentar") boton.disabled = preferencias.texto >= maximo;
                if (accion === "reducir") boton.disabled = preferencias.texto <= minimo;
                if (accion === "oscuro" || accion === "contraste") {
                    boton.setAttribute("aria-pressed", String(preferencias[accion]));
                }
            }
            if (estado) estado.textContent = "Texto: " + Math.round(factores[String(preferencias.texto)] * 100) + " %";
        }

        for (const boton of botones) {
            boton.addEventListener("click", function () {
                const accion = boton.getAttribute("data-nf-a11y");
                if (accion === "aumentar") preferencias.texto = Math.min(maximo, preferencias.texto + 1);
                if (accion === "reducir") preferencias.texto = Math.max(minimo, preferencias.texto - 1);
                if (accion === "oscuro" || accion === "contraste") preferencias[accion] = !preferencias[accion];
                guardar();
                aplicarColores();
                if (accion === "aumentar" || accion === "reducir") aplicarTexto();
                actualizarControles();
            });
        }

        aplicarTexto();
        actualizarControles();
    }

    aplicarColores();
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", iniciar);
    } else {
        iniciar();
    }
})();
