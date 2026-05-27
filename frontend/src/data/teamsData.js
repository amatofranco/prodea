// flag: código ISO para flagcdn.com
// player: jugador más representativo

export const TEAMS = {
  // CONMEBOL
  'Argentina':         { flag: 'ar', player: 'L. Messi' },
  'Brasil':            { flag: 'br', player: 'Vinícius Jr.' },
  'Colombia':          { flag: 'co', player: 'L. Díaz' },
  'Ecuador':           { flag: 'ec', player: 'E. Valencia' },
  'Paraguay':          { flag: 'py', player: 'M. Almirón' },
  'Uruguay':           { flag: 'uy', player: 'D. Núñez' },
  // UEFA
  'Alemania':          { flag: 'de', player: 'F. Wirtz' },
  'Austria':           { flag: 'at', player: 'M. Sabitzer' },
  'Bélgica':           { flag: 'be', player: 'K. De Bruyne' },
  'Bosnia y Herzegovina': { flag: 'ba', player: 'E. Džeko' },
  'Croacia':           { flag: 'hr', player: 'L. Modrić' },
  'España':            { flag: 'es', player: 'L. Yamal' },
  'Escocia':           { flag: 'gb-sct', player: 'A. Robertson' },
  'Francia':           { flag: 'fr', player: 'K. Mbappé' },
  'Inglaterra':        { flag: 'gb-eng', player: 'J. Bellingham' },
  'Países Bajos':      { flag: 'nl', player: 'V. van Dijk' },
  'Noruega':           { flag: 'no', player: 'E. Haaland' },
  'Portugal':          { flag: 'pt', player: 'C. Ronaldo' },
  'República Checa':   { flag: 'cz', player: 'P. Schick' },
  'Suecia':            { flag: 'se', player: 'A. Isak' },
  'Suiza':             { flag: 'ch', player: 'G. Xhaka' },
  'Turquía':           { flag: 'tr', player: 'H. Çalhanoğlu' },
  // CAF
  'Argelia':           { flag: 'dz', player: 'R. Mahrez' },
  'Cabo Verde':        { flag: 'cv', player: 'G. Martins' },
  'Costa de Marfil':   { flag: 'ci', player: 'S. Haller' },
  'Egipto':            { flag: 'eg', player: 'M. Salah' },
  'Ghana':             { flag: 'gh', player: 'M. Kudus' },
  'Marruecos':         { flag: 'ma', player: 'A. Hakimi' },
  'R. D. del Congo':   { flag: 'cd', player: 'C. Bakambu' },
  'Senegal':           { flag: 'sn', player: 'S. Mané' },
  'Sudáfrica':         { flag: 'za', player: 'P. Tau' },
  'Túnez':             { flag: 'tn', player: 'E. Skhiri' },
  // AFC
  'Arabia Saudita':    { flag: 'sa', player: 'S. Al-Dawsari' },
  'Australia':         { flag: 'au', player: 'M. Leckie' },
  'Catar':             { flag: 'qa', player: 'A. Afif' },
  'Corea del Sur':     { flag: 'kr', player: 'Son Heung-min' },
  'Irak':              { flag: 'iq', player: 'A. Hussein' },
  'Irán':              { flag: 'ir', player: 'M. Taremi' },
  'Japón':             { flag: 'jp', player: 'R. Dōan' },
  'Jordania':          { flag: 'jo', player: 'M. Al-Taamari' },
  'Uzbekistán':        { flag: 'uz', player: 'E. Shomurodov' },
  // CONCACAF
  'Canadá':            { flag: 'ca', player: 'A. Davies' },
  'Curazao':           { flag: 'cw', player: 'J. Bacuna' },
  'Estados Unidos':    { flag: 'us', player: 'C. Pulisic' },
  'Haití':             { flag: 'ht', player: 'F. Delva' },
  'México':            { flag: 'mx', player: 'C. Lozano' },
  'Panamá':            { flag: 'pa', player: 'R. Blackburn' },
  // OFC
  'Nueva Zelanda':     { flag: 'nz', player: 'C. Wood' },
  // Fallback para TBD
  'TBD':               { flag: null, player: '' },
}

export function getTeam(name) {
  return TEAMS[name] ?? { flag: null, player: name }
}

export function getFlagUrl(code) {
  if (!code) return null
  return `https://flagcdn.com/h60/${code}.png`
}
