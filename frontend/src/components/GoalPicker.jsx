import { useRef, useState, useCallback } from 'react'
import { ChevronUp, ChevronDown } from 'lucide-react'
import { AnimatePresence, motion } from 'framer-motion'

export default function GoalPicker({ value, onChange, disabled }) {
  const [direction, setDirection] = useState(1)
  const touchStartY = useRef(null)
  const dragStartValue = useRef(null)

  function change(delta) {
    if (disabled) return
    const next = Math.max(0, Math.min(9, value + delta))
    if (next !== value) {
      setDirection(delta)
      onChange(next)
    }
  }

  function onTouchStart(e) {
    touchStartY.current = e.touches[0].clientY
    dragStartValue.current = value
  }

  function onTouchMove(e) {
    if (touchStartY.current === null || disabled) return
    const dy = touchStartY.current - e.touches[0].clientY
    const delta = Math.round(dy / 28)
    const next = Math.max(0, Math.min(9, dragStartValue.current + delta))
    if (next !== value) {
      setDirection(delta > 0 ? 1 : -1)
      onChange(next)
    }
  }

  function onWheel(e) {
    e.preventDefault()
    change(e.deltaY > 0 ? -1 : 1)
  }

  const variants = {
    enter: (dir) => ({ y: dir > 0 ? -24 : 24, opacity: 0 }),
    center: { y: 0, opacity: 1 },
    exit: (dir) => ({ y: dir > 0 ? 24 : -24, opacity: 0 }),
  }

  return (
    <div className="flex flex-col items-center select-none">
      <button
        onPointerDown={() => change(1)}
        disabled={disabled}
        className="w-12 h-10 flex items-center justify-center text-[#00FF87] active:scale-90 transition-transform disabled:opacity-30"
      >
        <ChevronUp size={28} strokeWidth={2.5} />
      </button>

      <div
        className="w-20 h-20 relative flex items-center justify-center overflow-hidden cursor-ns-resize"
        onTouchStart={onTouchStart}
        onTouchMove={onTouchMove}
        onTouchEnd={() => { touchStartY.current = null }}
        onWheel={onWheel}
      >
        <div className="absolute inset-0 rounded-2xl bg-[#1A1A2E] border border-[#2A2A3E]" />
        <AnimatePresence mode="popLayout" custom={direction}>
          <motion.span
            key={value}
            custom={direction}
            variants={variants}
            initial="enter"
            animate="center"
            exit="exit"
            transition={{ duration: 0.15, ease: [0.34, 1.56, 0.64, 1] }}
            className="relative z-10 font-display text-5xl text-white leading-none"
            style={{ fontFamily: 'Bebas Neue, Barlow Condensed, sans-serif' }}
          >
            {value}
          </motion.span>
        </AnimatePresence>
      </div>

      <button
        onPointerDown={() => change(-1)}
        disabled={disabled}
        className="w-12 h-10 flex items-center justify-center text-[#00FF87] active:scale-90 transition-transform disabled:opacity-30"
      >
        <ChevronDown size={28} strokeWidth={2.5} />
      </button>
    </div>
  )
}
