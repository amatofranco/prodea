export const pointsColor = (pts) => {
  if (pts >= 4) return 'text-[#A855F7]'
  if (pts === 3) return 'text-[#00FF87]'
  if (pts > 0)  return 'text-[#F59E0B]'
  return 'text-[#8A8A9A]'
}

export const pointsBg = (pts) => {
  if (pts >= 4) return 'bg-[#A855F7]/10 border-[#A855F7]/30'
  if (pts === 3) return 'bg-[#00FF87]/10 border-[#00FF87]/30'
  if (pts > 0)  return 'bg-[#F59E0B]/10 border-[#F59E0B]/30'
  return 'bg-[#1A1A2E] border-[#2A2A3E]'
}
