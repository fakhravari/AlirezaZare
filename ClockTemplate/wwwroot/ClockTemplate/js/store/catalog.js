window.HNProducts = Array.from({ length: 30 }, (_, i) => {
  const brands = [
    "ویکتورینوکس",
    "پتک فیلیپ",
    "پلیس",
    "پولو",
    "پولو اکسچنج",
    "بورلی هیلز",
  ];
  const types = ["مردانه", "زنانه", "اسپرت", "کلاسیک"];
  const movements = ["کوارتز", "اتوماتیک", "مکانیکی", "سولار"];
  const straps = ["استیل", "چرم طبیعی", "سیلیکونی", "بند مش"];
  const images = [
    "ClockTemplate/images/products/watch-blue.webp",
    "ClockTemplate/images/products/watch-cream.webp",
    "ClockTemplate/images/products/watch-silver.webp",
    "ClockTemplate/images/products/watch-story.webp",
    "ClockTemplate/images/products/watch-hero.webp",
  ];
  const names = [
    "نوا کرونو",
    "آرمان کلاسیک",
    "مهتاب مینیمال",
    "هاله رزگلد",
    "میراث اتوماتیک",
    "آبان استیل",
  ];
  const englishNames = ['Nova Chrono', 'Arman Classic', 'Mahtab Minimal', 'Haleh Rose Gold', 'Miras Automatic', 'Aban Steel'];
  return {
    id: i + 1,
    englishName: `${englishNames[i % englishNames.length]} ${i > 5 ? i + 1 : ""}`.trim(),
    name: `${names[i % names.length]} ${i > 5 ? i + 1 : ""}`.trim(),
    brand: brands[i % brands.length],
    type: types[i % types.length],
    movement: movements[i % movements.length],
    strap: straps[i % straps.length],
    price: i === 0 ? 24.5 : 14 + ((i * 3) % 45),
    image: images[i % images.length],
    newest: 30 - i,
    badge: i % 7 === 0 ? "پرفروش" : i % 5 === 0 ? "جدید" : "",
  };
});

