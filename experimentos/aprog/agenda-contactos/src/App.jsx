import { useEffect, useMemo, useState } from 'react'
import './App.css'

const SIZE = 4
const BEST_KEY = 'game-2048-best-score'

function createEmptyBoard() {
  return Array.from({ length: SIZE }, () => Array(SIZE).fill(0))
}

function cloneBoard(board) {
  return board.map((row) => [...row])
}

function getEmptyCells(board) {
  const cells = []
  for (let row = 0; row < SIZE; row += 1) {
    for (let col = 0; col < SIZE; col += 1) {
      if (board[row][col] === 0) cells.push([row, col])
    }
  }
  return cells
}

function addRandomTile(board) {
  const emptyCells = getEmptyCells(board)
  if (emptyCells.length === 0) return board

  const [row, col] = emptyCells[Math.floor(Math.random() * emptyCells.length)]
  board[row][col] = Math.random() < 0.9 ? 2 : 4
  return board
}

function createInitialBoard() {
  const board = createEmptyBoard()
  addRandomTile(board)
  addRandomTile(board)
  return board
}

function slideLine(line) {
  const values = line.filter((value) => value !== 0)
  const merged = []
  let score = 0

  for (let index = 0; index < values.length; index += 1) {
    if (values[index] === values[index + 1]) {
      const combined = values[index] * 2
      merged.push(combined)
      score += combined
      index += 1
    } else {
      merged.push(values[index])
    }
  }

  while (merged.length < SIZE) merged.push(0)

  const changed = merged.some((value, index) => value !== line[index])
  return { line: merged, score, changed }
}

function moveLeft(board) {
  const nextBoard = board.map((row) => {
    const result = slideLine(row)
    return result.line
  })

  const score = board.reduce((total, row) => total + slideLine(row).score, 0)
  const changed = board.some((row, index) => row.some((value, col) => value !== nextBoard[index][col]))

  return { board: nextBoard, score, changed }
}

function reverseRows(board) {
  return board.map((row) => [...row].reverse())
}

function transpose(board) {
  return board[0].map((_, col) => board.map((row) => row[col]))
}

function moveBoard(board, direction) {
  let working = cloneBoard(board)

  if (direction === 'right') {
    working = reverseRows(working)
    const result = moveLeft(working)
    return { board: reverseRows(result.board), score: result.score, changed: result.changed }
  }

  if (direction === 'up') {
    working = transpose(working)
    const result = moveLeft(working)
    return { board: transpose(result.board), score: result.score, changed: result.changed }
  }

  if (direction === 'down') {
    working = transpose(working)
    working = reverseRows(working)
    const result = moveLeft(working)
    return {
      board: transpose(reverseRows(result.board)),
      score: result.score,
      changed: result.changed,
    }
  }

  return moveLeft(working)
}

function hasWon(board) {
  return board.some((row) => row.some((value) => value >= 2048))
}

function canMove(board) {
  if (getEmptyCells(board).length > 0) return true

  for (let row = 0; row < SIZE; row += 1) {
    for (let col = 0; col < SIZE; col += 1) {
      const value = board[row][col]
      if (row + 1 < SIZE && board[row + 1][col] === value) return true
      if (col + 1 < SIZE && board[row][col + 1] === value) return true
    }
  }

  return false
}

function getTileClass(value) {
  return `tile tile-${value || 0}`
}

function App() {
  const [board, setBoard] = useState(() => createInitialBoard())
  const [score, setScore] = useState(0)
  const [best, setBest] = useState(() => Number(localStorage.getItem(BEST_KEY) || 0))
  const [gameOver, setGameOver] = useState(false)
  const [won, setWon] = useState(false)

  const status = useMemo(() => {
    if (gameOver) return 'Perdiste. Probá otra vez.'
    if (won) return '¡Llegaste a 2048! Podés seguir jugando.'
    return 'Usá flechas o deslizá con WASD.'
  }, [gameOver, won])

  function resetGame() {
    setBoard(createInitialBoard())
    setScore(0)
    setGameOver(false)
    setWon(false)
  }

  function playMove(direction) {
    if (gameOver) return

    const result = moveBoard(board, direction)
    if (!result.changed) return

    const nextBoard = addRandomTile(cloneBoard(result.board))
    const nextScore = score + result.score
    const nextWon = won || hasWon(nextBoard)
    const nextGameOver = !canMove(nextBoard)

    setBoard(nextBoard)
    setScore(nextScore)
    setWon(nextWon)
    setGameOver(nextGameOver)
    setBest((currentBest) => {
      const nextBest = Math.max(currentBest, nextScore)
      localStorage.setItem(BEST_KEY, String(nextBest))
      return nextBest
    })
  }

  useEffect(() => {
    function handleKeyDown(event) {
      const map = {
        ArrowLeft: 'left',
        ArrowRight: 'right',
        ArrowUp: 'up',
        ArrowDown: 'down',
        a: 'left',
        d: 'right',
        w: 'up',
        s: 'down',
      }

      const direction = map[event.key]
      if (!direction) return
      event.preventDefault()
      playMove(direction)
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [board, score, gameOver, won])

  return (
    <main className="app">
      <section className="panel">
        <div className="header">
          <div>
            <h1>2048</h1>
            <p className="subtitle">Un juego web autocontenido en React.</p>
          </div>
          <button className="restart" onClick={resetGame}>
            Nuevo juego
          </button>
        </div>

        <div className="scoreboard">
          <div className="score-card">
            <span>Puntaje</span>
            <strong>{score}</strong>
          </div>
          <div className="score-card">
            <span>Mejor</span>
            <strong>{best}</strong>
          </div>
        </div>

        <p className="status">{status}</p>

        <div className="board" role="grid" aria-label="Tablero 2048">
          {board.flatMap((row, rowIndex) =>
            row.map((value, colIndex) => (
              <div
                key={`${rowIndex}-${colIndex}`}
                className={getTileClass(value)}
                role="gridcell"
                aria-label={value === 0 ? 'Celda vacía' : `Ficha ${value}`}
              >
                {value !== 0 ? value : ''}
              </div>
            )),
          )}
        </div>

        <div className="help">
          <p>Controles: ↑ ↓ ← → o W A S D</p>
          <p>Objetivo: combinar fichas hasta llegar a 2048.</p>
        </div>
      </section>
    </main>
  )
}

export default App
