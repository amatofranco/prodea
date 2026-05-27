// flag: código ISO para flagcdn.com

export const TEAMS = {
  // CONMEBOL
  'Argentina':            { flag: 'ar' },
  'Brasil':               { flag: 'br' },
  'Colombia':             { flag: 'co' },
  'Ecuador':              { flag: 'ec' },
  'Paraguay':             { flag: 'py' },
  'Peru':                 { flag: 'pe' },
  'Perú':                 { flag: 'pe' },
  'Uruguay':              { flag: 'uy' },
  'Venezuela':            { flag: 've' },
  // UEFA
  'Alemania':             { flag: 'de' },
  'Austria':              { flag: 'at' },
  'Bélgica':              { flag: 'be' },
  'Bosnia y Herzegovina': { flag: 'ba' },
  'Croacia':              { flag: 'hr' },
  'Dinamarca':            { flag: 'dk' },
  'España':               { flag: 'es' },
  'Escocia':              { flag: 'gb-sct' },
  'Francia':              { flag: 'fr' },
  'Gales':                { flag: 'gb-wls' },
  'Inglaterra':           { flag: 'gb-eng' },
  'Italia':               { flag: 'it' },
  'Países Bajos':         { flag: 'nl' },
  'Polonia':              { flag: 'pl' },
  'Noruega':              { flag: 'no' },
  'Portugal':             { flag: 'pt' },
  'República Checa':      { flag: 'cz' },
  'Rumania':              { flag: 'ro' },
  'Serbia':               { flag: 'rs' },
  'Suecia':               { flag: 'se' },
  'Suiza':                { flag: 'ch' },
  'Turquía':              { flag: 'tr' },
  // CAF
  'Argelia':              { flag: 'dz' },
  'Cabo Verde':           { flag: 'cv' },
  'Camerún':              { flag: 'cm' },
  'Costa de Marfil':      { flag: 'ci' },
  'Egipto':               { flag: 'eg' },
  'Ghana':                { flag: 'gh' },
  'Marruecos':            { flag: 'ma' },
  'Nigeria':              { flag: 'ng' },
  'R. D. del Congo':      { flag: 'cd' },
  'Senegal':              { flag: 'sn' },
  'Sudáfrica':            { flag: 'za' },
  'Túnez':                { flag: 'tn' },
  // AFC
  'Arabia Saudita':       { flag: 'sa' },
  'Australia':            { flag: 'au' },
  'Catar':                { flag: 'qa' },
  'Corea del Sur':        { flag: 'kr' },
  'Irak':                 { flag: 'iq' },
  'Irán':                 { flag: 'ir' },
  'Japón':                { flag: 'jp' },
  'Jordania':             { flag: 'jo' },
  'Uzbekistán':           { flag: 'uz' },
  // CONCACAF
  'Canadá':               { flag: 'ca' },
  'Costa Rica':           { flag: 'cr' },
  'Curazao':              { flag: 'cw' },
  'El Salvador':          { flag: 'sv' },
  'Estados Unidos':       { flag: 'us' },
  'Haití':                { flag: 'ht' },
  'Honduras':             { flag: 'hn' },
  'Jamaica':              { flag: 'jm' },
  'México':               { flag: 'mx' },
  'Panamá':               { flag: 'pa' },
  // OFC
  'Nueva Zelanda':        { flag: 'nz' },
  // Fallback
  'TBD':                  { flag: null },
}

export function getTeam(name) {
  return TEAMS[name] ?? { flag: null }
}

export function getFlagUrl(code) {
  if (!code) return null
  return `https://flagcdn.com/h60/${code}.png`
}
