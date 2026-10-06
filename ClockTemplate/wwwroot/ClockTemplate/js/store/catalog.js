window.HNProducts = Array.from({ length: 30 }, (_, i) => {
  const brands = [
    "بوش",
    "دیوالت",
    "ماکیتا",
    "رونیکس",
    "توتال",
    "کنزاکس",
  ];
  const types = ["ابزار برقی", "اندازه‌گیری", "تعمیرات", "روشنایی"];
  const movements = ["برقی", "شارژی", "دیجیتال", "دستی"];
  const straps = ["کارگاهی", "خانگی", "صنعتی", "تعمیرگاهی"];
  const images = [
    "ClockTemplate/images/products/tool-hammer-drill.png",
    "ClockTemplate/images/products/tool-multimeter-kit.png",
    "ClockTemplate/images/products/tool-soldering-station.png",
    "ClockTemplate/images/products/tool-led-worklight.png",
  ];
  const names = [
    "دریل چکشی شارژی",
    "کیت مولتی‌متر دیجیتال",
    "ایستگاه هویه حرفه‌ای",
    "چراغ کار LED",
    "مجموعه ابزار برق‌کار",
    "تستر و فازمتر دیجیتال",
  ];
  const englishNames = ['Hammer Drill', 'Digital Multimeter', 'Soldering Station', 'LED Work Light', 'Electrician Tool Set', 'Digital Tester'];
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

