import { useMemo, useState } from 'react'
import './App.css'

const now = new Date()

const eventosIniciales = [
  {
    id: 1,
    hora: '08:30',
    titulo: 'Revisar correo y prioridades',
    tipo: 'Personal',
    lugar: 'Casa',
    detalle: 'Confirmar tareas críticas y responder mensajes urgentes.',
    estado: 'En curso',
  },
  {
    id: 2,
    hora: '10:00',
    titulo: 'Reunión con equipo de proyecto',
    tipo: 'Trabajo',
    lugar: 'Sala Naranja',
    detalle: 'Definir entregables de la semana y revisar bloqueos.',
    estado: 'Próximo',
  },
  {
    id: 3,
    hora: '12:30',
    titulo: 'Almuerzo con Laura',
    tipo: 'Social',
    lugar: 'Café Central',
    detalle: 'Poner al día la agenda y coordinar el viaje del viernes.',
    estado: 'Confirmado',
  },
  {
    id: 4,
    hora: '16:00',
    titulo: 'Llamada con proveedor',
    tipo: 'Trabajo',
    lugar: 'Videollamada',
    detalle: 'Cerrar el presupuesto y validar fechas de entrega.',
    estado: 'Pendiente',
  },
  {
    id: 5,
    hora: '19:15',
    titulo: 'Gimnasio',
    tipo: 'Bienestar',
    lugar: 'Club Norte',
    detalle: 'Entrenamiento de fuerza y estiramientos.',
    estado: 'Agendado',
  },
]

const recordatorios = [
  'Llevar cargador y documento',
  'Comprar pan al volver',
  'Responder presupuesto pendiente',
]

const proximosDias = ['Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb', 'Dom']

function formatoFecha(fecha) {
  return new Intl.DateTimeFormat('es-AR', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
  }).format(fecha)
}

function App() {
  const [eventos, setEventos] = useState(eventosIniciales)
  const [seleccionadoId, setSeleccionadoId] = useState(eventosIniciales[1].id)
  const [nuevoTitulo, setNuevoTitulo] = useState('')
  const [nuevaHora, setNuevaHora] = useState('09:00')

  const seleccionado = useMemo(
    () => eventos.find((evento) => evento.id === seleccionadoId) ?? eventos[0],
    [eventos, seleccionadoId],
  )

  const totalHoy = eventos.length
  const proximos = eventos.filter((evento) => evento.estado !== 'En curso').length

  function agregarEvento(e) {
    e.preventDefault()
    const titulo = nuevoTitulo.trim()
    if (!titulo) return

    const nuevoEvento = {
      id: Date.now(),
      hora: nuevaHora,
      titulo,
      tipo: 'Personal',
      lugar: 'Por definir',
      detalle: 'Evento agregado desde la misma página.',
      estado: 'Nuevo',
    }

    setEventos((actuales) => [nuevoEvento, ...actuales].sort((a, b) => a.hora.localeCompare(b.hora)))
    setSeleccionadoId(nuevoEvento.id)
    setNuevoTitulo('')
    setNuevaHora('09:00')
  }

  return (
    <main className="app-shell">
      <section className="agenda-page" aria-label="Agenda del día">
        <aside className="sidebar">
          <div className="hero">
            <p className="eyebrow">Agenda personal</p>
            <h1>{formatoFecha(now)}</h1>
            <p className="hero-copy">
              Organizá tu jornada, revisá tus citas y sumá nuevos eventos sin salir de esta página.
            </p>
          </div>

          <div className="stats">
            <article>
              <span>Eventos</span>
              <strong>{totalHoy}</strong>
            </article>
            <article>
              <span>Próximos</span>
              <strong>{proximos}</strong>
            </article>
            <article>
              <span>Estado</span>
              <strong>Activo</strong>
            </article>
          </div>

          <div className="mini-calendar" aria-label="Próximos días">
            {proximosDias.map((dia, index) => {
              const activo = index === 2
              return (
                <div className={`day-pill ${activo ? 'is-active' : ''}`} key={dia}>
                  <span>{dia}</span>
                  <strong>{index + 12}</strong>
                </div>
              )
            })}
          </div>

          <form className="quick-add" onSubmit={agregarEvento}>
            <h2>Agregar evento</h2>
            <label>
              Título
              <input
                value={nuevoTitulo}
                onChange={(e) => setNuevoTitulo(e.target.value)}
                placeholder="Ej. Estudio, reunión, gym"
              />
            </label>
            <label>
              Hora
              <input value={nuevaHora} onChange={(e) => setNuevaHora(e.target.value)} type="time" />
            </label>
            <button type="submit">Sumar a la agenda</button>
          </form>
        </aside>

        <section className="content">
          <header className="content-header">
            <div>
              <p className="eyebrow">Día de hoy</p>
              <h2>Eventos programados</h2>
            </div>
            <div className="legend" aria-label="Leyenda de estados">
              <span><i className="dot dot-now" />En curso</span>
              <span><i className="dot dot-next" />Próximo</span>
              <span><i className="dot dot-new" />Nuevo</span>
            </div>
          </header>

          <div className="timeline" role="list" aria-label="Lista de eventos">
            {eventos.map((evento) => {
              const activo = evento.id === seleccionado.id

              return (
                <button
                  key={evento.id}
                  className={`event-card ${activo ? 'is-selected' : ''}`}
                  onClick={() => setSeleccionadoId(evento.id)}
                  type="button"
                  role="listitem"
                  aria-current={activo ? 'true' : undefined}
                >
                  <span className="event-time">{evento.hora}</span>
                  <div className="event-body">
                    <div className="event-topline">
                      <strong>{evento.titulo}</strong>
                      <span>{evento.tipo}</span>
                    </div>
                    <p>{evento.lugar}</p>
                  </div>
                </button>
              )
            })}
          </div>

          <article className="detail-panel" aria-labelledby="detail-title">
            <div className="detail-header">
              <div className="detail-time">{seleccionado.hora}</div>
              <div>
                <p className="eyebrow">{seleccionado.estado}</p>
                <h3 id="detail-title">{seleccionado.titulo}</h3>
              </div>
            </div>

            <div className="detail-grid">
              <div className="info-box">
                <span>Tipo</span>
                <strong>{seleccionado.tipo}</strong>
              </div>
              <div className="info-box">
                <span>Lugar</span>
                <strong>{seleccionado.lugar}</strong>
              </div>
              <div className="info-box wide">
                <span>Detalle</span>
                <strong>{seleccionado.detalle}</strong>
              </div>
            </div>

            <div className="notes">
              <h4>Recordatorios</h4>
              <ul>
                {recordatorios.map((item) => (
                  <li key={item}>{item}</li>
                ))}
              </ul>
            </div>
          </article>
        </section>
      </section>
    </main>
  )
}

export default App
