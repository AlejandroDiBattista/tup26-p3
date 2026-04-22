/* 2048 — Glassmorphism Warm Edition */
const { useState, useEffect, useRef, useCallback, useMemo } = React;

// ---------- Tweakable defaults ----------
const TWEAK_DEFAULTS = /*EDITMODE-BEGIN*/{
  "tileShape": "rounded",
  "theme": "sunset"
}/*EDITMODE-END*/;

// ---------- Themes (all warm) ----------
const THEMES = {
  sunset: {
    name: "Sunset",
    bg: "radial-gradient(ellipse at 20% 10%, #ffd8a8 0%, transparent 50%), radial-gradient(ellipse at 80% 90%, #ff9a76 0%, transparent 55%), radial-gradient(ellipse at 50% 50%, #ffe5c4 0%, #fbc39a 100%)",
    blobs: ["#ff7a59", "#ffb566", "#ffd28a"],
    ink: "#4a2a1a",
    inkSoft: "rgba(74, 42, 26, 0.55)",
    glass: "rgba(255, 255, 255, 0.28)",
    glassBorder: "rgba(255, 255, 255, 0.55)",
    boardGlass: "rgba(255, 250, 244, 0.22)",
    cellGlass: "rgba(255, 255, 255, 0.18)",
    cellBorder: "rgba(255, 255, 255, 0.35)"
  },
  peach: {
    name: "Peach Cream",
    bg: "radial-gradient(ellipse at 30% 20%, #fff1e0 0%, transparent 55%), radial-gradient(ellipse at 70% 80%, #ffc9b5 0%, transparent 55%), linear-gradient(135deg, #ffeadb 0%, #ffd1b8 100%)",
    blobs: ["#ff9a8b", "#ffc3a0", "#ffe1cc"],
    ink: "#5a2e1e",
    inkSoft: "rgba(90, 46, 30, 0.55)",
    glass: "rgba(255, 255, 255, 0.35)",
    glassBorder: "rgba(255, 255, 255, 0.6)",
    boardGlass: "rgba(255, 250, 244, 0.3)",
    cellGlass: "rgba(255, 255, 255, 0.22)",
    cellBorder: "rgba(255, 255, 255, 0.4)"
  },
  amber: {
    name: "Amber Glow",
    bg: "radial-gradient(ellipse at 15% 15%, #ffd27a 0%, transparent 50%), radial-gradient(ellipse at 85% 85%, #e8763a 0%, transparent 55%), linear-gradient(135deg, #fbcb80 0%, #d65c3a 100%)",
    blobs: ["#ff6b35", "#ffa645", "#ffd27a"],
    ink: "#3a1a0a",
    inkSoft: "rgba(58, 26, 10, 0.6)",
    glass: "rgba(255, 240, 220, 0.25)",
    glassBorder: "rgba(255, 220, 180, 0.5)",
    boardGlass: "rgba(255, 235, 210, 0.2)",
    cellGlass: "rgba(255, 240, 220, 0.15)",
    cellBorder: "rgba(255, 220, 180, 0.3)"
  }
};

// ---------- Tile color map (warm gradient from pale yellow to deep cherry) ----------
const TILE_STYLES = {
  2:    { bg: "linear-gradient(135deg, #fff4d6 0%, #ffe5a8 100%)", ring: "rgba(255, 220, 150, 0.9)", ink: "#6b4a1f", glow: "#ffe9b5" },
  4:    { bg: "linear-gradient(135deg, #ffe5b0 0%, #ffd080 100%)", ring: "rgba(255, 200, 120, 0.9)", ink: "#6b3e16", glow: "#ffd59a" },
  8:    { bg: "linear-gradient(135deg, #ffcf8a 0%, #ffb35c 100%)", ring: "rgba(255, 175, 90, 0.95)", ink: "#fff", glow: "#ffb560" },
  16:   { bg: "linear-gradient(135deg, #ffb06a 0%, #ff8a3d 100%)", ring: "rgba(255, 140, 60, 0.95)", ink: "#fff", glow: "#ff914a" },
  32:   { bg: "linear-gradient(135deg, #ff8f5a 0%, #ff6a3d 100%)", ring: "rgba(255, 110, 70, 0.95)", ink: "#fff", glow: "#ff7548" },
  64:   { bg: "linear-gradient(135deg, #ff7557 0%, #f04a32 100%)", ring: "rgba(240, 80, 60, 0.95)", ink: "#fff", glow: "#f75638" },
  128:  { bg: "linear-gradient(135deg, #ff5a7a 0%, #e6334f 100%)", ring: "rgba(230, 60, 90, 1)",    ink: "#fff", glow: "#ff4765" },
  256:  { bg: "linear-gradient(135deg, #f04080 0%, #c8256c 100%)", ring: "rgba(220, 60, 120, 1)",   ink: "#fff", glow: "#e63480" },
  512:  { bg: "linear-gradient(135deg, #d63090 0%, #a31e7c 100%)", ring: "rgba(200, 50, 150, 1)",   ink: "#fff", glow: "#c72b88" },
  1024: { bg: "linear-gradient(135deg, #b82a9a 0%, #7e1c80 100%)", ring: "rgba(170, 40, 160, 1)",   ink: "#fff", glow: "#a72590" },
  2048: { bg: "linear-gradient(135deg, #ffd86b 0%, #ff6b35 50%, #e6334f 100%)", ring: "rgba(255, 200, 80, 1)", ink: "#fff", glow: "#ffb347" },
  4096: { bg: "linear-gradient(135deg, #2c0f3a 0%, #6b2a80 50%, #ff6b35 100%)", ring: "rgba(255, 150, 80, 1)", ink: "#fff", glow: "#ff9a4a" },
  8192: { bg: "linear-gradient(135deg, #000 0%, #4a1a60 50%, #ff3a3a 100%)",    ring: "rgba(255, 80, 80, 1)",  ink: "#fff", glow: "#ff4a4a" }
};

const SIZE = 4;
const STORAGE_KEY = "glass2048.state.v1";
const BEST_KEY = "glass2048.best.v1";
const PERFORMANCE_STYLE_ID = "glass2048-performance-style";

// ---------- Audio feedback ----------
function createAudioEngine() {
  const AudioCtx = window.AudioContext || window.webkitAudioContext;
  let ctx = null;

  function getCtx() {
    if (!AudioCtx) return null;
    if (!ctx) ctx = new AudioCtx();
    if (ctx.state === "suspended") ctx.resume().catch(() => {});
    return ctx;
  }

  function envelope(gainNode, now, attack, peak, decay, endValue = 0.0001) {
    gainNode.gain.cancelScheduledValues(now);
    gainNode.gain.setValueAtTime(0.0001, now);
    gainNode.gain.exponentialRampToValueAtTime(peak, now + attack);
    gainNode.gain.exponentialRampToValueAtTime(endValue, now + attack + decay);
  }

  function playMerge(mergeCount, gained) {
    const audio = getCtx();
    if (!audio) return;

    const now = audio.currentTime + 0.01;
    const voices = Math.min(3, Math.max(1, mergeCount));
    const base = Math.min(880, 360 + Math.log2(Math.max(4, gained || 4)) * 26);
    const intervals = [0, 4, 7];

    for (let i = 0; i < voices; i++) {
      const osc = audio.createOscillator();
      const gain = audio.createGain();
      const filter = audio.createBiquadFilter();

      osc.type = i === 0 ? "sine" : "triangle";
      const freq = base * Math.pow(2, intervals[i] / 12);
      osc.frequency.setValueAtTime(freq, now + i * 0.03);
      osc.frequency.exponentialRampToValueAtTime(freq * 1.08, now + i * 0.03 + 0.12);

      filter.type = "lowpass";
      filter.frequency.setValueAtTime(1800 + i * 300, now);
      filter.Q.value = 1;

      envelope(gain, now + i * 0.03, 0.015, 0.07 / (i + 1), 0.22);

      osc.connect(filter);
      filter.connect(gain);
      gain.connect(audio.destination);

      osc.start(now + i * 0.03);
      osc.stop(now + i * 0.03 + 0.28);
    }
  }

  function playBlocked() {
    const audio = getCtx();
    if (!audio) return;

    const now = audio.currentTime + 0.01;
    const osc = audio.createOscillator();
    const gain = audio.createGain();
    const filter = audio.createBiquadFilter();

    osc.type = "triangle";
    osc.frequency.setValueAtTime(160, now);
    osc.frequency.exponentialRampToValueAtTime(68, now + 0.11);

    filter.type = "lowpass";
    filter.frequency.setValueAtTime(420, now);
    filter.Q.value = 0.8;

    envelope(gain, now, 0.008, 0.055, 0.1);

    osc.connect(filter);
    filter.connect(gain);
    gain.connect(audio.destination);

    osc.start(now);
    osc.stop(now + 0.14);
  }

  return {
    playMerge,
    playBlocked,
    dispose() {
      if (ctx && ctx.state !== "closed") ctx.close().catch(() => {});
      ctx = null;
    }
  };
}

function ensurePerformanceStyles() {
  if (document.getElementById(PERFORMANCE_STYLE_ID)) return;

  const style = document.createElement("style");
  style.id = PERFORMANCE_STYLE_ID;
  style.textContent = `
    .blobs {
      filter: none !important;
      opacity: 0.42 !important;
    }

    .blob {
      will-change: transform;
    }

    .score-card,
    .btn-ghost,
    .board-wrap,
    .cell,
    .overlay,
    .overlay-card,
    .tweaks-panel {
      backdrop-filter: none !important;
      -webkit-backdrop-filter: none !important;
    }

    .board-wrap {
      contain: layout style;
    }

    .grid-bg,
    .tiles-layer {
      contain: layout;
    }

    .tile {
      left: 0 !important;
      top: 0 !important;
      transition: transform 0.16s cubic-bezier(0.4, 0.0, 0.2, 1) !important;
    }

    .tile-inner {
      transform: translateZ(0);
    }
  `;
  document.head.appendChild(style);
}

// ---------- Game logic ----------
let _idCounter = 1;
const newId = () => _idCounter++;

function emptyBoard() {
  return Array.from({ length: SIZE }, () => Array(SIZE).fill(null));
}

function cloneBoard(b) {
  return b.map(row => row.map(cell => cell ? { ...cell } : null));
}

function spawnTile(board) {
  const empties = [];
  for (let r = 0; r < SIZE; r++) for (let c = 0; c < SIZE; c++) if (!board[r][c]) empties.push([r, c]);
  if (empties.length === 0) return board;
  const [r, c] = empties[Math.floor(Math.random() * empties.length)];
  const value = Math.random() < 0.9 ? 2 : 4;
  board[r][c] = { id: newId(), value, isNew: true, merged: false };
  return board;
}

function initBoard() {
  const b = emptyBoard();
  spawnTile(b);
  spawnTile(b);
  return b;
}

// Move row left, return [newRow, scoreGained, moved, mergePositions]
function slideRow(row) {
  const filtered = row.filter(Boolean).map(t => ({ ...t, merged: false, isNew: false }));
  const result = [];
  let score = 0;
  const mergeIndices = [];
  for (let i = 0; i < filtered.length; i++) {
    if (i + 1 < filtered.length && filtered[i].value === filtered[i + 1].value && !filtered[i].merged) {
      const newVal = filtered[i].value * 2;
      const mergedTile = { id: newId(), value: newVal, merged: true, isNew: false };
      result.push(mergedTile);
      score += newVal;
      mergeIndices.push(result.length - 1);
      i++;
    } else {
      result.push(filtered[i]);
    }
  }
  while (result.length < SIZE) result.push(null);
  const moved = row.some((t, i) => (t?.id ?? null) !== (result[i]?.id ?? null) || (t?.value ?? 0) !== (result[i]?.value ?? 0));
  return { row: result, score, moved, mergeIndices };
}

function rotateCW(board) {
  const n = board.length;
  const out = emptyBoard();
  for (let r = 0; r < n; r++) for (let c = 0; c < n; c++) out[c][n - 1 - r] = board[r][c];
  return out;
}
function rotateCCW(board) {
  const n = board.length;
  const out = emptyBoard();
  for (let r = 0; r < n; r++) for (let c = 0; c < n; c++) out[n - 1 - c][r] = board[r][c];
  return out;
}

function move(board, direction) {
  // rotate so the move is effectively "left"
  let b = cloneBoard(board);
  let restore = (nextBoard) => nextBoard;
  if (direction === "up") {
    b = rotateCCW(b);
    restore = rotateCW;
  } else if (direction === "right") {
    b = rotateCW(rotateCW(b));
    restore = (nextBoard) => rotateCW(rotateCW(nextBoard));
  } else if (direction === "down") {
    b = rotateCW(b);
    restore = rotateCCW;
  }

  let totalScore = 0;
  let anyMoved = false;
  let mergeCount = 0;
  for (let r = 0; r < SIZE; r++) {
    const { row, score, moved, mergeIndices } = slideRow(b[r]);
    b[r] = row;
    totalScore += score;
    if (moved) anyMoved = true;
    mergeCount += mergeIndices.length;
  }

  // rotate back
  b = restore(b);
  return { board: b, score: totalScore, moved: anyMoved, mergeCount };
}

function hasMoves(board) {
  for (let r = 0; r < SIZE; r++) for (let c = 0; c < SIZE; c++) {
    if (!board[r][c]) return true;
    const v = board[r][c].value;
    if (c + 1 < SIZE && board[r][c + 1] && board[r][c + 1].value === v) return true;
    if (r + 1 < SIZE && board[r + 1][c] && board[r + 1][c].value === v) return true;
  }
  return false;
}

function maxValue(board) {
  let m = 0;
  for (const row of board) for (const cell of row) if (cell && cell.value > m) m = cell.value;
  return m;
}

// ---------- Main App ----------
function Game() {
  const [tweaks, setTweaks] = useState(TWEAK_DEFAULTS);
  const [editMode, setEditMode] = useState(false);

  const [board, setBoard] = useState(() => {
    try {
      const saved = JSON.parse(localStorage.getItem(STORAGE_KEY) || "null");
      if (saved?.board) {
        // ensure ids fresh
        saved.board.forEach(row => row.forEach(cell => { if (cell) { cell.id = newId(); cell.isNew = false; cell.merged = false; }}));
        return saved.board;
      }
    } catch (e) {}
    return initBoard();
  });
  const [score, setScore] = useState(() => {
    try { return JSON.parse(localStorage.getItem(STORAGE_KEY) || "null")?.score || 0; } catch { return 0; }
  });
  const [best, setBest] = useState(() => {
    try { return parseInt(localStorage.getItem(BEST_KEY) || "0", 10) || 0; } catch { return 0; }
  });
  const [history, setHistory] = useState([]);
  const [shake, setShake] = useState(0);
  const [scorePop, setScorePop] = useState(0);
  const [confetti, setConfetti] = useState([]);
  const [hasWon, setHasWon] = useState(false);
  const [showWin, setShowWin] = useState(false);
  const [gameOver, setGameOver] = useState(false);
  const [lastGain, setLastGain] = useState(null);

  const theme = THEMES[tweaks.theme] || THEMES.sunset;
  const boardRef = useRef(null);
  const busyRef = useRef(false);
  const audioRef = useRef(null);

  useEffect(() => {
    audioRef.current = createAudioEngine();
    return () => audioRef.current?.dispose?.();
  }, []);

  useEffect(() => {
    ensurePerformanceStyles();
  }, []);

  // Persist
  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ board, score }));
  }, [board, score]);
  useEffect(() => {
    if (score > best) { setBest(score); localStorage.setItem(BEST_KEY, String(score)); }
  }, [score, best]);

  // Check win / game over
  useEffect(() => {
    const mx = maxValue(board);
    if (mx >= 2048 && !hasWon) { setHasWon(true); setShowWin(true); launchConfetti(160); }
    if (!hasMoves(board)) setGameOver(true);
    else setGameOver(false);
  }, [board]);

  const launchConfetti = useCallback((count = 60) => {
    const pieces = [];
    const colors = ["#ff6b35", "#ffb347", "#ffd86b", "#ff4765", "#e63480", "#ffffff"];
    for (let i = 0; i < count; i++) {
      pieces.push({
        id: newId(),
        left: 50 + (Math.random() - 0.5) * 20,
        top: 50 + (Math.random() - 0.5) * 10,
        dx: (Math.random() - 0.5) * 600,
        dy: -300 - Math.random() * 400,
        rot: Math.random() * 720 - 360,
        color: colors[Math.floor(Math.random() * colors.length)],
        size: 6 + Math.random() * 8,
        shape: Math.random() > 0.5 ? "rect" : "circ",
        dur: 1400 + Math.random() * 800
      });
    }
    setConfetti(pieces);
    setTimeout(() => setConfetti([]), 2400);
  }, []);

  const tryMove = useCallback((dir) => {
    if (busyRef.current) return;
    const prevBoard = cloneBoard(board);
    const prevScore = score;
    const { board: next, score: gained, moved, mergeCount } = move(board, dir);
    if (!moved) {
      audioRef.current?.playBlocked();
      setShake(s => s + 1);
      setTimeout(() => setShake(s => s), 100);
      return;
    }
    busyRef.current = true;
    // Keep move-result, then spawn after animation
    setBoard(next);
    if (gained > 0) {
      audioRef.current?.playMerge(mergeCount, gained);
      setScore(s => s + gained);
      setLastGain({ amount: gained, id: newId() });
      setScorePop(p => p + 1);
    }
    setHistory(h => [...h.slice(-9), { board: prevBoard, score: prevScore }]);

    if (mergeCount >= 2) {
      setShake(s => s + 1);
    }

    setTimeout(() => {
      setBoard(b => {
        const nb = cloneBoard(b);
        // clear merged/new flags
        for (let r = 0; r < SIZE; r++) for (let c = 0; c < SIZE; c++) {
          if (nb[r][c]) { nb[r][c].merged = false; nb[r][c].isNew = false; }
        }
        spawnTile(nb);
        return nb;
      });
      busyRef.current = false;
    }, 140);
  }, [board, score]);

  // Keyboard
  useEffect(() => {
    const onKey = (e) => {
      const k = e.key;
      if (k === "ArrowLeft" || k === "a" || k === "A") { e.preventDefault(); tryMove("left"); }
      else if (k === "ArrowRight" || k === "d" || k === "D") { e.preventDefault(); tryMove("right"); }
      else if (k === "ArrowUp" || k === "w" || k === "W") { e.preventDefault(); tryMove("up"); }
      else if (k === "ArrowDown" || k === "s" || k === "S") { e.preventDefault(); tryMove("down"); }
      else if (k === "z" || k === "Z") { undo(); }
      else if (k === "r" || k === "R") { reset(); }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [tryMove]);

  // Swipe
  useEffect(() => {
    const el = boardRef.current;
    if (!el) return;
    let startX = 0, startY = 0, tracking = false;
    const onStart = (e) => {
      const t = e.touches ? e.touches[0] : e;
      startX = t.clientX; startY = t.clientY; tracking = true;
    };
    const onEnd = (e) => {
      if (!tracking) return;
      tracking = false;
      const t = e.changedTouches ? e.changedTouches[0] : e;
      const dx = t.clientX - startX;
      const dy = t.clientY - startY;
      const absX = Math.abs(dx), absY = Math.abs(dy);
      if (Math.max(absX, absY) < 24) return;
      if (absX > absY) tryMove(dx > 0 ? "right" : "left");
      else tryMove(dy > 0 ? "down" : "up");
    };
    el.addEventListener("touchstart", onStart, { passive: true });
    el.addEventListener("touchend", onEnd, { passive: true });
    return () => {
      el.removeEventListener("touchstart", onStart);
      el.removeEventListener("touchend", onEnd);
    };
  }, [tryMove]);

  const undo = useCallback(() => {
    setHistory(h => {
      if (h.length === 0) return h;
      const last = h[h.length - 1];
      setBoard(last.board);
      setScore(last.score);
      setHasWon(false); // allow re-win celebration only once anyway; keep simple
      return h.slice(0, -1);
    });
  }, []);

  const reset = useCallback(() => {
    setBoard(initBoard());
    setScore(0);
    setHistory([]);
    setHasWon(false);
    setShowWin(false);
    setGameOver(false);
    setConfetti([]);
  }, []);

  // --- Tweaks bridge ---
  useEffect(() => {
    const onMsg = (e) => {
      const d = e.data;
      if (!d || typeof d !== "object") return;
      if (d.type === "__activate_edit_mode") setEditMode(true);
      if (d.type === "__deactivate_edit_mode") setEditMode(false);
    };
    window.addEventListener("message", onMsg);
    window.parent.postMessage({ type: "__edit_mode_available" }, "*");
    return () => window.removeEventListener("message", onMsg);
  }, []);

  const updateTweak = (key, value) => {
    setTweaks(t => {
      const nt = { ...t, [key]: value };
      window.parent.postMessage({ type: "__edit_mode_set_keys", edits: { [key]: value } }, "*");
      return nt;
    });
  };

  // Build flat tile list with positions for animation
  const tiles = useMemo(() => {
    const list = [];
    for (let r = 0; r < SIZE; r++) for (let c = 0; c < SIZE; c++) {
      const t = board[r][c];
      if (t) list.push({ ...t, r, c });
    }
    return list;
  }, [board]);

  // Shape radius
  const tileRadius = tweaks.tileShape === "circle" ? "50%" : tweaks.tileShape === "square" ? "6px" : "18px";
  const cellRadius = tweaks.tileShape === "circle" ? "50%" : tweaks.tileShape === "square" ? "6px" : "16px";

  return (
    <div className="app" style={{ background: theme.bg, "--ink": theme.ink, "--ink-soft": theme.inkSoft }}>
      {/* Background blobs */}
      <div className="blobs" aria-hidden="true">
        <div className="blob" style={{ background: theme.blobs[0], top: "-10%", left: "-5%" }} />
        <div className="blob" style={{ background: theme.blobs[1], bottom: "-15%", right: "-10%" }} />
        <div className="blob" style={{ background: theme.blobs[2], top: "40%", left: "60%" }} />
      </div>

      <div className="container">
        <header className="header">
          <div className="brand">
            <div className="brand-mark" aria-hidden="true">
              <div className="brand-mark-inner">
                <span>2</span><span>0</span><span>4</span><span>8</span>
              </div>
            </div>
            <div className="brand-text">
              <h1>2048</h1>
              <p>Combina las fichas, alcanza <em>2048</em>.</p>
            </div>
          </div>

          <div className="scores">
            <ScoreCard label="Puntos" value={score} pop={scorePop} gain={lastGain} theme={theme} />
            <ScoreCard label="Mejor" value={best} theme={theme} />
          </div>
        </header>

        <div className="controls-bar">
          <button className="btn btn-ghost" onClick={undo} disabled={history.length === 0}>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round"><path d="M3 7v6h6"/><path d="M21 17a9 9 0 0 0-15-6.7L3 13"/></svg>
            Deshacer
          </button>
          <div className="hint">
            <kbd>←</kbd><kbd>↑</kbd><kbd>↓</kbd><kbd>→</kbd> o desliza
          </div>
          <button className="btn btn-primary" onClick={reset}>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round"><path d="M3 12a9 9 0 1 0 3-6.7"/><path d="M3 3v6h6"/></svg>
            Nueva partida
          </button>
        </div>

        <div
          ref={boardRef}
          className={"board-wrap " + (shake ? "shake-" + (shake % 2) : "")}
          style={{
            background: theme.boardGlass,
            borderColor: theme.glassBorder,
          }}
        >
          {/* Grid cells (background) */}
          <div className="grid-bg">
            {Array.from({ length: SIZE * SIZE }).map((_, i) => (
              <div key={i} className="cell" style={{ background: theme.cellGlass, borderColor: theme.cellBorder, borderRadius: cellRadius }} />
            ))}
          </div>
          {/* Tiles layer */}
          <div className="tiles-layer">
            {tiles.map(t => (
              <Tile key={t.id} tile={t} radius={tileRadius} />
            ))}
          </div>

          {/* Overlays */}
          {gameOver && !showWin && (
            <div className="overlay">
              <div className="overlay-card" style={{ background: theme.glass, borderColor: theme.glassBorder }}>
                <div className="overlay-eyebrow">fin del camino</div>
                <div className="overlay-title">No hay movimientos</div>
                <div className="overlay-sub">Puntos: <b>{score}</b></div>
                <button className="btn btn-primary" onClick={reset}>Intentar de nuevo</button>
              </div>
            </div>
          )}
          {showWin && (
            <div className="overlay">
              <div className="overlay-card win" style={{ background: theme.glass, borderColor: theme.glassBorder }}>
                <div className="overlay-eyebrow">🏆 lo lograste</div>
                <div className="overlay-title">¡2048!</div>
                <div className="overlay-sub">Puedes seguir jugando o empezar de nuevo.</div>
                <div className="overlay-actions">
                  <button className="btn btn-ghost" onClick={reset}>Nueva partida</button>
                  <button className="btn btn-primary" onClick={() => setShowWin(false)}>Seguir jugando</button>
                </div>
              </div>
            </div>
          )}
        </div>

        <footer className="foot">
          <div className="foot-left">
            Une fichas iguales. Dobla. Repite. <span>Atajos: <kbd>Z</kbd> deshacer · <kbd>R</kbd> reiniciar</span>
          </div>
        </footer>
      </div>

      {/* Confetti */}
      <div className="confetti-layer" aria-hidden="true">
        {confetti.map(p => (
          <div
            key={p.id}
            className="confetti-piece"
            style={{
              left: p.left + "%",
              top: p.top + "%",
              background: p.color,
              width: p.size, height: p.size,
              borderRadius: p.shape === "circ" ? "50%" : "2px",
              "--dx": p.dx + "px",
              "--dy": p.dy + "px",
              "--rot": p.rot + "deg",
              animationDuration: p.dur + "ms"
            }}
          />
        ))}
      </div>

      {/* Tweaks panel */}
      {editMode && (
        <TweaksPanel tweaks={tweaks} onChange={updateTweak} theme={theme} />
      )}
    </div>
  );
}

// ---------- ScoreCard ----------
function ScoreCard({ label, value, pop, gain, theme }) {
  return (
    <div className="score-card" style={{ background: theme.glass, borderColor: theme.glassBorder }}>
      <div className="score-label">{label}</div>
      <div className={"score-value" + (pop ? " pop-" + (pop % 2) : "")} key={pop}>
        {value.toLocaleString()}
      </div>
      {gain && label === "Puntos" && (
        <div key={gain.id} className="score-gain">+{gain.amount}</div>
      )}
    </div>
  );
}

// ---------- Tile ----------
function Tile({ tile, radius }) {
  const st = TILE_STYLES[tile.value] || TILE_STYLES[8192];
  const digits = String(tile.value).length;
  const fontSize = digits <= 2 ? "clamp(28px, 6.2vw, 56px)" : digits === 3 ? "clamp(24px, 5.2vw, 46px)" : digits === 4 ? "clamp(20px, 4.2vw, 38px)" : "clamp(16px, 3.4vw, 30px)";
  const glow = tile.value >= 128 ? `0 0 24px ${st.glow}55, 0 10px 30px rgba(0,0,0,0.18)` : `0 6px 18px rgba(74, 42, 26, 0.18)`;
  return (
    <div
      className={"tile" + (tile.isNew ? " new" : "") + (tile.merged ? " merged" : "")}
      style={{
        transform: `translate3d(calc(${tile.c} * (var(--cell) + var(--gap))), calc(${tile.r} * (var(--cell) + var(--gap))), 0)`,
      }}
    >
      <div
        className="tile-inner"
        style={{
          background: st.bg,
          color: st.ink,
          borderRadius: radius,
          boxShadow: `inset 0 1px 0 rgba(255,255,255,0.45), inset 0 -2px 6px rgba(0,0,0,0.1), ${glow}`,
          border: `1px solid ${st.ring}`
        }}
      >
        <span className="tile-num" style={{ fontSize }}>{tile.value}</span>
      </div>
    </div>
  );
}

// ---------- Tweaks Panel ----------
function TweaksPanel({ tweaks, onChange, theme }) {
  return (
    <div className="tweaks-panel" style={{ background: theme.glass, borderColor: theme.glassBorder }}>
      <div className="tweaks-head">
        <div className="tweaks-title">Tweaks</div>
        <div className="tweaks-sub">Ajusta el look en vivo</div>
      </div>

      <div className="tweaks-section">
        <div className="tweaks-label">Tema</div>
        <div className="tweaks-row">
          {Object.entries(THEMES).map(([k, v]) => (
            <button
              key={k}
              onClick={() => onChange("theme", k)}
              className={"swatch " + (tweaks.theme === k ? "active" : "")}
              style={{ background: `linear-gradient(135deg, ${v.blobs[0]}, ${v.blobs[1]}, ${v.blobs[2]})` }}
              title={v.name}
            >
              <span>{v.name}</span>
            </button>
          ))}
        </div>
      </div>

      <div className="tweaks-section">
        <div className="tweaks-label">Forma de ficha</div>
        <div className="tweaks-row">
          {[
            { k: "rounded", label: "Redondeada", r: "18px" },
            { k: "square",  label: "Cuadrada",   r: "6px" },
            { k: "circle",  label: "Círculo",    r: "50%" }
          ].map(opt => (
            <button
              key={opt.k}
              onClick={() => onChange("tileShape", opt.k)}
              className={"shape-opt " + (tweaks.tileShape === opt.k ? "active" : "")}
            >
              <span className="shape-preview" style={{ borderRadius: opt.r }} />
              {opt.label}
            </button>
          ))}
        </div>
      </div>
    </div>
  );
}

// Mount
const root = ReactDOM.createRoot(document.getElementById("root"));
root.render(<Game />);
