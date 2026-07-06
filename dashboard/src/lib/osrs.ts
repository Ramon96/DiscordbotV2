// OSRS experience table: cumulative XP required to reach a given skill level.
// XP(L) = floor( (1/4) * sum_{n=1}^{L-1} floor( n + 300 * 2^(n/7) ) )
const MAX_SKILL_LEVEL = 99

function cumulativeXpForLevel(level: number): number {
  let points = 0
  for (let n = 1; n < level; n++) {
    points += Math.floor(n + 300 * Math.pow(2, n / 7))
  }
  return Math.floor(points / 4)
}

const xpTable: number[] = Array.from({ length: MAX_SKILL_LEVEL + 1 }, (_, level) =>
  cumulativeXpForLevel(level),
)

export function xpForLevel(level: number): number {
  if (level <= 1) {
    return 0
  }
  return level <= MAX_SKILL_LEVEL ? xpTable[level] : cumulativeXpForLevel(level)
}

/** XP remaining to the next level, or null once a skill is maxed (99). */
export function xpToNextLevel(level: number, experience: number): number | null {
  if (level >= MAX_SKILL_LEVEL) {
    return null
  }
  return Math.max(0, xpForLevel(level + 1) - experience)
}

/** Fraction (0–1) of progress through the current level, or null when maxed. */
export function levelProgress(level: number, experience: number): number | null {
  if (level >= MAX_SKILL_LEVEL) {
    return null
  }
  const start = xpForLevel(level)
  const end = xpForLevel(level + 1)
  if (end <= start) {
    return null
  }
  return Math.min(1, Math.max(0, (experience - start) / (end - start)))
}
