// Condiciones: 1 → Libre | 2 → Regular | 3 → Promoción TP | 4 → Ap. Directa
// C7: 7I9-1FI-O8Q
// C9: 7I9-1FI-OTB
const alumnos = [
    { legajo: 61490, condicion: 2, nota:  0, comision: 7 },
    { legajo: 61577, condicion: 1, nota:  0, comision: 7 },
    { legajo: 61581, condicion: 2, nota:  0, comision: 7 },
    { legajo: 63174, condicion: 2, nota:  0, comision: 7 },
    { legajo: 63208, condicion: 2, nota:  0, comision: 7 },
    { legajo: 63211, condicion: 1, nota:  0, comision: 7 },
    { legajo: 63241, condicion: 2, nota:  0, comision: 7 },
    { legajo: 63268, condicion: 2, nota:  0, comision: 7 },
    { legajo: 63350, condicion: 2, nota:  0, comision: 7 },
    { legajo: 63354, condicion: 2, nota:  0, comision: 7 },
    { legajo: 63387, condicion: 4, nota:  9, comision: 7 },
    { legajo: 63389, condicion: 2, nota:  0, comision: 7 },
    { legajo: 63393, condicion: 4, nota: 10, comision: 7 },
    { legajo: 63396, condicion: 2, nota:  0, comision: 7 },
    { legajo: 63397, condicion: 2, nota:  0, comision: 7 },
    { legajo: 63399, condicion: 4, nota: 10, comision: 7 },
    { legajo: 63402, condicion: 2, nota:  0, comision: 7 },
    { legajo: 63415, condicion: 1, nota:  0, comision: 7 },
    { legajo: 63419, condicion: 2, nota:  0, comision: 7 },
    { legajo: 63420, condicion: 2, nota:  0, comision: 7 },
    { legajo: 63447, condicion: 4, nota: 10, comision: 7 },
    { legajo: 63456, condicion: 2, nota:  0, comision: 7 },
    { legajo: 63457, condicion: 4, nota: 10, comision: 7 },
    { legajo: 63546, condicion: 4, nota: 10, comision: 7 },
    { legajo: 63547, condicion: 4, nota: 10, comision: 7 },
    { legajo: 63647, condicion: 2, nota:  0, comision: 7 },
    { legajo: 63700, condicion: 2, nota:  0, comision: 7 },
    { legajo: 63776, condicion: 2, nota:  0, comision: 7 },
    { legajo: 61026, condicion: 4, nota:  9, comision: 9 },
    { legajo: 61057, condicion: 2, nota:  0, comision: 9 },
    { legajo: 61161, condicion: 2, nota:  0, comision: 9 },
    { legajo: 61489, condicion: 4, nota:  9, comision: 9 },
    { legajo: 61641, condicion: 2, nota:  0, comision: 9 },
    { legajo: 61801, condicion: 2, nota:  0, comision: 9 },
    { legajo: 61907, condicion: 1, nota:  0, comision: 9 },
    { legajo: 62844, condicion: 1, nota:  0, comision: 9 },
    { legajo: 63137, condicion: 2, nota:  0, comision: 9 },
    { legajo: 63150, condicion: 2, nota:  0, comision: 9 },
    { legajo: 63182, condicion: 4, nota: 10, comision: 9 },
    { legajo: 63205, condicion: 1, nota:  0, comision: 9 },
    { legajo: 63207, condicion: 4, nota: 10, comision: 9 },
    { legajo: 63213, condicion: 2, nota:  0, comision: 9 },
    { legajo: 63216, condicion: 2, nota:  0, comision: 9 },
    { legajo: 63217, condicion: 2, nota:  0, comision: 9 },
    { legajo: 63218, condicion: 4, nota: 10, comision: 9 },
    { legajo: 63219, condicion: 2, nota:  0, comision: 9 },
    { legajo: 63220, condicion: 2, nota:  0, comision: 9 },
    { legajo: 63222, condicion: 2, nota:  0, comision: 9 },
    { legajo: 63231, condicion: 2, nota:  0, comision: 9 },
    { legajo: 63232, condicion: 2, nota:  0, comision: 9 },
    { legajo: 63234, condicion: 2, nota:  0, comision: 9 },
    { legajo: 63266, condicion: 2, nota:  0, comision: 9 },
    { legajo: 63297, condicion: 4, nota: 10, comision: 9 },
    { legajo: 63300, condicion: 4, nota: 10, comision: 9 },
    { legajo: 63313, condicion: 4, nota: 10, comision: 9 },
    { legajo: 63341, condicion: 4, nota: 10, comision: 9 },
    { legajo: 63345, condicion: 2, nota:  0, comision: 9 },
    { legajo: 63385, condicion: 2, nota:  0, comision: 9 },
    { legajo: 63388, condicion: 4, nota: 10, comision: 9 },
    { legajo: 63412, condicion: 2, nota:  0, comision: 9 },
    { legajo: 63418, condicion: 2, nota:  0, comision: 9 },
    { legajo: 63425, condicion: 2, nota:  0, comision: 9 },
    { legajo: 63461, condicion: 2, nota:  0, comision: 9 },
    { legajo: 63493, condicion: 2, nota:  0, comision: 9 },
    { legajo: 63494, condicion: 4, nota: 10, comision: 9 },
    { legajo: 63717, condicion: 4, nota:  9, comision: 9 },
    { legajo: 63737, condicion: 4, nota: 10, comision: 9 },
    { legajo: 64016, condicion: 2, nota:  0, comision: 9 },
]

const $    = (selector, origen=document) => origen?.querySelector(selector) ?? null
const fila = legajo => $(`input[name="legajo"][value="${legajo}"]`)?.closest('tr')

const comision    = $('.tituloTabla').textContent.match(/Comisión\s*(\d+)/)[1]
const enCondicion = $('select[name="nota"]')  // Cuando esta editando la condición
const enNota      = $('input[name="nota"]')   // Cuando esta editando la nota

console.log(`=== Comisión [${comision}] | Condicion: ${enCondicion} | Notas: ${enNota} ===`)
for(const {legajo, condicion, nota} of alumnos.filter(a => a.comision == comision)){
    const destino = fila(legajo)
    if (enCondicion) {
        $('select[name="nota"]', destino).selectedIndex = condicion;
    }
    if (enNota) {
        $('input[name="nota"]', destino).value = condicion == 4 ? nota : "";
    }
}
