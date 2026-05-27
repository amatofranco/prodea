// flag: código ISO para flagcdn.com
// player: jugador más representativo
// img: foto del jugador (Wikimedia Commons — onError muestra la bandera)

export const TEAMS = {
  // CONMEBOL
  'Argentina':         { flag: 'ar', player: 'L. Messi',        img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/b/b4/Lionel-Messi-Argentina-2022-FIFA-World-Cup_%28cropped%29.jpg/440px-Lionel-Messi-Argentina-2022-FIFA-World-Cup_%28cropped%29.jpg' },
  'Brasil':            { flag: 'br', player: 'Vinícius Jr.',     img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/9/9e/Vinicius_Junior_2023_%28cropped%29.jpg/440px-Vinicius_Junior_2023_%28cropped%29.jpg' },
  'Colombia':          { flag: 'co', player: 'L. Díaz',          img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/c/c7/FC_RB_Salzburg_gegen_FC_Bayern_M%C3%BCnchen_%282026-01-06_Testspiel%29_40_%28Luiz_D%C3%ADaz%29.jpg/440px-FC_RB_Salzburg_gegen_FC_Bayern_M%C3%BCnchen_%282026-01-06_Testspiel%29_40_%28Luiz_D%C3%ADaz%29.jpg' },
  'Ecuador':           { flag: 'ec', player: 'E. Valencia',      img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/8/88/Enner_Valencia_CONTINUACI%C3%93N_DE_LA_SESI%C3%93N_N.%C2%BA_056_DEL_PLENO_DE_LA_ASAMBLEA_NACIONAL._ECUADOR%2C_09_DE_DICIEMBRE_DE_2025_%28cropped%29.jpg/440px-Enner_Valencia_CONTINUACI%C3%93N_DE_LA_SESI%C3%93N_N.%C2%BA_056_DEL_PLENO_DE_LA_ASAMBLEA_NACIONAL._ECUADOR%2C_09_DE_DICIEMBRE_DE_2025_%28cropped%29.jpg' },
  'Paraguay':          { flag: 'py', player: 'M. Almirón',       img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/1/19/Miguel_Almir%C3%B3n_Red_Bull_Atlanta_5.31.25-069_%28cropped%29.jpg/440px-Miguel_Almir%C3%B3n_Red_Bull_Atlanta_5.31.25-069_%28cropped%29.jpg' },
  'Uruguay':           { flag: 'uy', player: 'D. Núñez',         img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/1/11/Darwin_N%C3%BA%C3%B1ez_%28cropped%29.jpg/440px-Darwin_N%C3%BA%C3%B1ez_%28cropped%29.jpg' },
  // UEFA
  'Alemania':          { flag: 'de', player: 'F. Wirtz',         img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/5/5d/Florian_Wirtz%2C_2022-07-31%2C_Saisoner%C3%B6ffnung_Bayer_04%2C_Leverkusen_%281%29_%28cropped%29.jpg/440px-Florian_Wirtz%2C_2022-07-31%2C_Saisoner%C3%B6ffnung_Bayer_04%2C_Leverkusen_%281%29_%28cropped%29.jpg' },
  'Austria':           { flag: 'at', player: 'M. Sabitzer',      img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/0/0e/Marcel_Sabitzer_2020_%28cropped%29.jpg/440px-Marcel_Sabitzer_2020_%28cropped%29.jpg' },
  'Bélgica':           { flag: 'be', player: 'K. De Bruyne',     img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/e/e4/Kevin_De_Bruyne_2021.jpg/440px-Kevin_De_Bruyne_2021.jpg' },
  'Bosnia y Herzegovina': { flag: 'ba', player: 'E. Džeko',      img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/2/2c/20150331_2026_AUT_BIH_2177_Edin_D%C5%BEeko_%28cropped%29.jpg/440px-20150331_2026_AUT_BIH_2177_Edin_D%C5%BEeko_%28cropped%29.jpg' },
  'Croacia':           { flag: 'hr', player: 'L. Modrić',        img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/7/7e/Luka_Modri%C4%87_2019_%28cropped%29.jpg/440px-Luka_Modri%C4%87_2019_%28cropped%29.jpg' },
  'España':            { flag: 'es', player: 'L. Yamal',         img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/e/e3/Lamine_Yamal_in_2025.jpg/440px-Lamine_Yamal_in_2025.jpg' },
  'Escocia':           { flag: 'gb-sct', player: 'A. Robertson', img: 'https://upload.wikimedia.org/wikipedia/commons/4/40/First_Minister_meets_with_Scottish_National_Football_Team_%28cropped_2%29.jpg' },
  'Francia':           { flag: 'fr', player: 'K. Mbappé',        img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/5/57/2019-07-17_SG_Dynamo_Dresden_vs_PSG_by_Sandro_Halank%E2%80%93108_%28cropped%29.jpg/440px-2019-07-17_SG_Dynamo_Dresden_vs_PSG_by_Sandro_Halank%E2%80%93108_%28cropped%29.jpg' },
  'Inglaterra':        { flag: 'gb-eng', player: 'J. Bellingham', img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/f/f9/25th_Laureus_World_Sports_Awards_-_Red_Carpet_-_Jude_Bellingham_-_240422_190551-2_%28cropped%29.jpg/440px-25th_Laureus_World_Sports_Awards_-_Red_Carpet_-_Jude_Bellingham_-_240422_190551-2_%28cropped%29.jpg' },
  'Países Bajos':      { flag: 'nl', player: 'V. van Dijk',      img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/5/5d/20160604_AUT_NED_8876_%28cropped%29.jpg/440px-20160604_AUT_NED_8876_%28cropped%29.jpg' },
  'Noruega':           { flag: 'no', player: 'E. Haaland',       img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/9/93/Erling_Haaland_2022_%28cropped%29.jpg/440px-Erling_Haaland_2022_%28cropped%29.jpg' },
  'Portugal':          { flag: 'pt', player: 'C. Ronaldo',       img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/8/8c/Cristiano_Ronaldo_2018.jpg/440px-Cristiano_Ronaldo_2018.jpg' },
  'República Checa':   { flag: 'cz', player: 'P. Schick',        img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/7/71/2020-03-10_Fu%C3%9Fball%2C_M%C3%A4nner%2C_UEFA_Champions_League_Achtelfinale%2C_RB_Leipzig_-_Tottenham_Hotspur_1DX_3672_by_Stepro.jpg/440px-2020-03-10_Fu%C3%9Fball%2C_M%C3%A4nner%2C_UEFA_Champions_League_Achtelfinale%2C_RB_Leipzig_-_Tottenham_Hotspur_1DX_3672_by_Stepro.jpg' },
  'Suecia':            { flag: 'se', player: 'A. Isak',           img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/0/00/UEFA_EURO_qualifiers_Sweden_vs_Spain_20191015_Alexander_Isak_56_%28cropped%29.jpg/440px-UEFA_EURO_qualifiers_Sweden_vs_Spain_20191015_Alexander_Isak_56_%28cropped%29.jpg' },
  'Suiza':             { flag: 'ch', player: 'G. Xhaka',          img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/0/01/Granit_Xhaka_%28cropped%29.jpg/440px-Granit_Xhaka_%28cropped%29.jpg' },
  'Turquía':           { flag: 'tr', player: 'H. Çalhanoğlu',    img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/d/d7/AUT_vs._TUR_2016-03-29_%28342%29.jpg/440px-AUT_vs._TUR_2016-03-29_%28342%29.jpg' },
  // CAF
  'Argelia':           { flag: 'dz', player: 'R. Mahrez',         img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/4/45/Mahrez_2021.jpg/440px-Mahrez_2021.jpg' },
  'Cabo Verde':        { flag: 'cv', player: 'G. Martins',        img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/a/a9/Cap-vert_vs_Ethiopi_%2824%29.jpg/440px-Cap-vert_vs_Ethiopi_%2824%29.jpg' },
  'Costa de Marfil':   { flag: 'ci', player: 'S. Haller',         img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/3/39/S%C3%A9bastien_Haller_2.jpg/440px-S%C3%A9bastien_Haller_2.jpg' },
  'Egipto':            { flag: 'eg', player: 'M. Salah',          img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/c/c1/Mohamed_Salah_2018.jpg/440px-Mohamed_Salah_2018.jpg' },
  'Ghana':             { flag: 'gh', player: 'M. Kudus',          img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/e/ef/Mohammed_Kudus_of_West_Ham_United.jpeg/440px-Mohammed_Kudus_of_West_Ham_United.jpeg' },
  'Marruecos':         { flag: 'ma', player: 'A. Hakimi',         img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/5/56/Achraf_Hakimi_%28cropped2%29.jpg/440px-Achraf_Hakimi_%28cropped2%29.jpg' },
  'R. D. del Congo':   { flag: 'cd', player: 'C. Bakambu',        img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/4/4b/C%C3%A9dric_Bakambu_2016_%28cropped%29.jpg/440px-C%C3%A9dric_Bakambu_2016_%28cropped%29.jpg' },
  'Senegal':           { flag: 'sn', player: 'S. Mané',           img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/1/1a/Sadio_Mane_Al-Nassr.jpg/440px-Sadio_Mane_Al-Nassr.jpg' },
  'Sudáfrica':         { flag: 'za', player: 'P. Tau',            img: 'https://upload.wikimedia.org/wikipedia/commons/f/f7/Percy_Tau_in_2019_%28cropped%29.jpg' },
  'Túnez':             { flag: 'tn', player: 'E. Skhiri',         img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/b/bf/2021-08-08_FC_Carl_Zeiss_Jena_gegen_1._FC_K%C3%B6ln_%28DFB-Pokal%29_by_Sandro_Halank%E2%80%93182_%28cropped2%29.jpg/440px-2021-08-08_FC_Carl_Zeiss_Jena_gegen_1._FC_K%C3%B6ln_%28DFB-Pokal%29_by_Sandro_Halank%E2%80%93182_%28cropped2%29.jpg' },
  // AFC
  'Arabia Saudita':    { flag: 'sa', player: 'S. Al-Dawsari',     img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/6/6d/Salem_Al-Dawsari_2018.jpg/440px-Salem_Al-Dawsari_2018.jpg' },
  'Australia':         { flag: 'au', player: 'M. Leckie',         img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/d/dd/Hertha_BSC_vs._West_Ham_United_20190731_%28054%29.jpg/440px-Hertha_BSC_vs._West_Ham_United_20190731_%28054%29.jpg' },
  'Catar':             { flag: 'qa', player: 'A. Afif',           img: 'https://upload.wikimedia.org/wikipedia/commons/c/c5/Qatar_v_Lebanon_%2837%29_%28cropped%29.jpg' },
  'Corea del Sur':     { flag: 'kr', player: 'Son Heung-min',     img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/a/aa/Son_Heung-min_2019_%28cropped%29.jpg/440px-Son_Heung-min_2019_%28cropped%29.jpg' },
  'Irak':              { flag: 'iq', player: 'A. Hussein',         img: null },
  'Irán':              { flag: 'ir', player: 'M. Taremi',          img: 'https://upload.wikimedia.org/wikipedia/commons/7/74/Iran_-_Japan%2C_AFC_Asian_Cup_2019_42_%28cropped%29.jpg' },
  'Japón':             { flag: 'jp', player: 'R. Dōan',            img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/1/17/Ritsu_Doan%2C_2019_AFC_Asian_Cup_1.jpg/440px-Ritsu_Doan%2C_2019_AFC_Asian_Cup_1.jpg' },
  'Jordania':          { flag: 'jo', player: 'M. Al-Taamari',     img: null },
  'Uzbekistán':        { flag: 'uz', player: 'E. Shomurodov',     img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/1/15/Eldor_Shomurodov_14_%C4%B0stanbul_Ba%C5%9Fak%C5%9Fehir_FK_20250731_%2810%29_%28cropped%29.jpg/440px-Eldor_Shomurodov_14_%C4%B0stanbul_Ba%C5%9Fak%C5%9Fehir_FK_20250731_%2810%29_%28cropped%29.jpg' },
  // CONCACAF
  'Canadá':            { flag: 'ca', player: 'A. Davies',          img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/f/ff/Alphonso_Davies_in_2022.jpg/440px-Alphonso_Davies_in_2022.jpg' },
  'Curazao':           { flag: 'cw', player: 'J. Bacuna',          img: null },
  'Estados Unidos':    { flag: 'us', player: 'C. Pulisic',         img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/8/84/Christian_Pulisic_2019_%28cropped%29.jpg/440px-Christian_Pulisic_2019_%28cropped%29.jpg' },
  'Haití':             { flag: 'ht', player: 'F. Delva',           img: null },
  'México':            { flag: 'mx', player: 'C. Lozano',          img: 'https://upload.wikimedia.org/wikipedia/commons/thumb/e/e3/Hirving_Lozano.png/440px-Hirving_Lozano.png' },
  'Panamá':            { flag: 'pa', player: 'R. Blackburn',       img: null },
  // OFC
  'Nueva Zelanda':     { flag: 'nz', player: 'C. Wood',            img: null },
  // Fallback para TBD
  'TBD':               { flag: null, player: '',                    img: null },
}

export function getTeam(name) {
  return TEAMS[name] ?? { flag: null, player: name, img: null }
}

export function getFlagUrl(code) {
  if (!code) return null
  return `https://flagcdn.com/h60/${code}.png`
}
