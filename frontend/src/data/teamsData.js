// flag: código ISO para flagcdn.com
// player: jugador más representativo
// img: foto del jugador (Wikipedia Special:FilePath — onError muestra la bandera)

export const TEAMS = {
  // CONMEBOL
  'Argentina':         { flag: 'ar', player: 'L. Messi',       img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/b/b4/Lionel-Messi-Argentina-2022-FIFA-World-Cup_%28cropped%29.jpg/440px-Lionel-Messi-Argentina-2022-FIFA-World-Cup_%28cropped%29.jpg' },
  'Brasil':            { flag: 'br', player: 'Vinícius Jr.',    img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/9/9e/Vinicius_Junior_2023_%28cropped%29.jpg/440px-Vinicius_Junior_2023_%28cropped%29.jpg' },
  'Colombia':          { flag: 'co', player: 'L. Díaz',         img: null },
  'Ecuador':           { flag: 'ec', player: 'E. Valencia',     img: null },
  'Paraguay':          { flag: 'py', player: 'M. Almirón',      img: null },
  'Uruguay':           { flag: 'uy', player: 'D. Núñez',        img: null },
  // UEFA
  'Alemania':          { flag: 'de', player: 'F. Wirtz',        img: null },
  'Austria':           { flag: 'at', player: 'M. Sabitzer',     img: null },
  'Bélgica':           { flag: 'be', player: 'K. De Bruyne',    img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/e/e4/Kevin_De_Bruyne_2021.jpg/440px-Kevin_De_Bruyne_2021.jpg' },
  'Bosnia y Herzegovina': { flag: 'ba', player: 'E. Džeko',    img: null },
  'Croacia':           { flag: 'hr', player: 'L. Modrić',       img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/7/7e/Luka_Modri%C4%87_2019_%28cropped%29.jpg/440px-Luka_Modri%C4%87_2019_%28cropped%29.jpg' },
  'España':            { flag: 'es', player: 'L. Yamal',        img: null },
  'Escocia':           { flag: 'gb-sct', player: 'A. Robertson', img: null },
  'Francia':           { flag: 'fr', player: 'K. Mbappé',       img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/5/57/2019-07-17_SG_Dynamo_Dresden_vs_PSG_by_Sandro_Halank%E2%80%93108_%28cropped%29.jpg/440px-2019-07-17_SG_Dynamo_Dresden_vs_PSG_by_Sandro_Halank%E2%80%93108_%28cropped%29.jpg' },
  'Inglaterra':        { flag: 'gb-eng', player: 'J. Bellingham', img: null },
  'Países Bajos':      { flag: 'nl', player: 'V. van Dijk',     img: null },
  'Noruega':           { flag: 'no', player: 'E. Haaland',      img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/9/93/Erling_Haaland_2022_%28cropped%29.jpg/440px-Erling_Haaland_2022_%28cropped%29.jpg' },
  'Portugal':          { flag: 'pt', player: 'C. Ronaldo',      img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/8/8c/Cristiano_Ronaldo_2018.jpg/440px-Cristiano_Ronaldo_2018.jpg' },
  'República Checa':   { flag: 'cz', player: 'P. Schick',       img: null },
  'Suecia':            { flag: 'se', player: 'A. Isak',          img: null },
  'Suiza':             { flag: 'ch', player: 'G. Xhaka',         img: null },
  'Turquía':           { flag: 'tr', player: 'H. Çalhanoğlu',   img: null },
  // CAF
  'Argelia':           { flag: 'dz', player: 'R. Mahrez',        img: null },
  'Cabo Verde':        { flag: 'cv', player: 'G. Martins',       img: null },
  'Costa de Marfil':   { flag: 'ci', player: 'S. Haller',        img: null },
  'Egipto':            { flag: 'eg', player: 'M. Salah',         img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/c/c1/Mohamed_Salah_2018.jpg/440px-Mohamed_Salah_2018.jpg' },
  'Ghana':             { flag: 'gh', player: 'M. Kudus',         img: null },
  'Marruecos':         { flag: 'ma', player: 'A. Hakimi',        img: null },
  'R. D. del Congo':   { flag: 'cd', player: 'C. Bakambu',       img: null },
  'Senegal':           { flag: 'sn', player: 'S. Mané',          img: null },
  'Sudáfrica':         { flag: 'za', player: 'P. Tau',           img: null },
  'Túnez':             { flag: 'tn', player: 'E. Skhiri',        img: null },
  // AFC
  'Arabia Saudita':    { flag: 'sa', player: 'S. Al-Dawsari',    img: null },
  'Australia':         { flag: 'au', player: 'M. Leckie',        img: null },
  'Catar':             { flag: 'qa', player: 'A. Afif',          img: null },
  'Corea del Sur':     { flag: 'kr', player: 'Son Heung-min',    img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/a/aa/Son_Heung-min_2019_%28cropped%29.jpg/440px-Son_Heung-min_2019_%28cropped%29.jpg' },
  'Irak':              { flag: 'iq', player: 'A. Hussein',        img: null },
  'Irán':              { flag: 'ir', player: 'M. Taremi',         img: null },
  'Japón':             { flag: 'jp', player: 'R. Dōan',           img: null },
  'Jordania':          { flag: 'jo', player: 'M. Al-Taamari',    img: null },
  'Uzbekistán':        { flag: 'uz', player: 'E. Shomurodov',    img: null },
  // CONCACAF
  'Canadá':            { flag: 'ca', player: 'A. Davies',         img: null },
  'Curazao':           { flag: 'cw', player: 'J. Bacuna',         img: null },
  'Estados Unidos':    { flag: 'us', player: 'C. Pulisic',        img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/8/84/Christian_Pulisic_2019_%28cropped%29.jpg/440px-Christian_Pulisic_2019_%28cropped%29.jpg' },
  'Haití':             { flag: 'ht', player: 'F. Delva',          img: null },
  'México':            { flag: 'mx', player: 'C. Lozano',         img: null },
  'Panamá':            { flag: 'pa', player: 'R. Blackburn',      img: null },
  // OFC
  'Nueva Zelanda':     { flag: 'nz', player: 'C. Wood',           img: null },
  // Fallback para TBD
  'TBD':               { flag: null, player: '',                   img: null },
}

export function getTeam(name) {
  return TEAMS[name] ?? { flag: null, player: name, img: null }
}

export function getFlagUrl(code) {
  if (!code) return null
  return `https://flagcdn.com/h60/${code}.png`
}
