import express from "express";

const app = express();
app.use(express.json());

const listings = [
  { id: "a1b2c3d4-e5f6-7890-abcd-ef1234567890", title: "Cozy Paris Apartment", country: "France", city: "Paris", pricePerNight: 120, capacity: 2, isAvailable: true },
  { id: "b2c3d4e5-f6a7-8901-bcde-f12345678901", title: "Modern Paris Studio", country: "France", city: "Paris", pricePerNight: 85, capacity: 1, isAvailable: true },
  { id: "c3d4e5f6-a7b8-9012-cdef-123456789012", title: "Istanbul Bosphorus View", country: "Turkey", city: "Istanbul", pricePerNight: 95, capacity: 3, isAvailable: true },
  { id: "d4e5f6a7-b8c9-0123-defa-234567890123", title: "Cozy Istanbul Flat", country: "Turkey", city: "Istanbul", pricePerNight: 60, capacity: 2, isAvailable: true },
  { id: "e5f6a7b8-c9d0-1234-efab-345678901234", title: "London Loft", country: "UK", city: "London", pricePerNight: 180, capacity: 4, isAvailable: true },
  { id: "f6a7b8c9-d0e1-2345-fabc-456789012345", title: "Rome Historic Center", country: "Italy", city: "Rome", pricePerNight: 110, capacity: 2, isAvailable: true }
];

app.get("/api/listings", (req, res) => {
  const { city, country, capacity, pageNumber = 1, pageSize = 10 } = req.query;
  let results = [...listings];
  if (city) results = results.filter(l => l.city.toLowerCase().includes(city.toLowerCase()));
  if (country) results = results.filter(l => l.country.toLowerCase().includes(country.toLowerCase()));
  if (capacity) results = results.filter(l => l.capacity >= Number(capacity));
  const page = Number(pageNumber);
  const size = Number(pageSize);
  const paged = results.slice((page - 1) * size, page * size);
  res.json({ items: paged, totalCount: results.length, pageNumber: page, pageSize: size });
});

app.post("/api/bookings", (req, res) => {
  res.status(201).json({ id: crypto.randomUUID(), ...req.body, status: "Confirmed" });
});

app.post("/api/reviews", (req, res) => {
  res.status(201).json({ id: crypto.randomUUID(), ...req.body });
});

app.listen(9090, () => console.log("Mock API ayakta: http://localhost:9090"));
