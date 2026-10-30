// ============================================================
// Проєкт №3: ReviewsDb — seed.js (тестові дані)
// Запуск (після collections.js): mongosh "mongodb://localhost:27017" seed.js
// Ідемпотентний: upsert за фіксованим _id ($setOnInsert), повторний запуск не створює дублікатів.
// ============================================================
const dbr = db.getSiblingDB("ReviewsDb");
const I = NumberInt;

// ===================== SEED (upsert за фіксованим _id) =====================
function up(coll, id, doc) {
  dbr[coll].updateOne({ _id: ObjectId(id) }, { $setOnInsert: doc }, { upsert: true });
}

const D1 = ObjectId("66f0c3000000000000000001");
const D2 = ObjectId("66f0c3000000000000000002");

// reviews: різні набори полів в одній колекції (гнучка схема)
up("reviews", "66f0c2000000000000000001", {          // повний: фото + коментарі + посилання на обговорення
  productId: I(1), productName: "Ноутбук X15",
  author: { userId: "u-17", displayName: "Олена" },
  rating: I(5), text: "Чудовий екран",
  photos: ["img1.jpg", "img2.jpg"],
  comments: [{ author: "Андрій", text: "Згоден", createdAt: ISODate("2026-09-01T10:00:00Z") }],
  discussionId: D1, createdAt: ISODate("2026-08-30T12:00:00Z")
});
up("reviews", "66f0c2000000000000000002", {          // мінімальний: лише оцінка та текст
  productId: I(1), productName: "Ноутбук X15",
  author: { userId: "u-21", displayName: "Іван" },
  rating: I(4), text: "Швидкий, але гарячий", createdAt: ISODate("2026-09-02T09:30:00Z")
});
up("reviews", "66f0c2000000000000000003", {          // інші поля: pros/cons, verifiedPurchase, videoUrl
  productId: I(5), productName: "Навушники H9",
  author: { userId: "u-17", displayName: "Олена" },
  rating: I(3), text: "Звук непоганий",
  pros: ["Автономність", "Легкі"], cons: ["Тисне на вуха"],
  verifiedPurchase: true, videoUrl: "https://example.com/v/h9",
  createdAt: ISODate("2026-09-05T18:10:00Z")
});
up("reviews", "66f0c2000000000000000004", {          // відгук без тексту, з розширеним автором
  productId: I(2), productName: "Бездротова миша M200",
  author: { userId: "u-33", displayName: "Максим", badge: "Top reviewer" },
  rating: I(5), createdAt: ISODate("2026-09-07T07:45:00Z")
});

// discussions + messages (reference)
up("discussions", "66f0c3000000000000000001", {
  productId: I(1), productName: "Ноутбук X15", title: "Охолодження під навантаженням",
  messageCount: I(3), lastMessageAt: ISODate("2026-09-03T11:00:00Z"), createdAt: ISODate("2026-08-31T10:00:00Z")
});
up("discussions", "66f0c3000000000000000002", {
  productId: I(5), productName: "Навушники H9", title: "Сумісність з iPhone",
  messageCount: I(1), lastMessageAt: ISODate("2026-09-06T12:00:00Z"), createdAt: ISODate("2026-09-06T12:00:00Z")
});
up("discussion_messages", "66f0c4000000000000000001", {
  discussionId: D1, parentId: null, author: { userId: "u-21", displayName: "Іван" },
  text: "У кого так само гріється?", createdAt: ISODate("2026-08-31T10:05:00Z")
});
up("discussion_messages", "66f0c4000000000000000002", {
  discussionId: D1, parentId: ObjectId("66f0c4000000000000000001"), author: { userId: "u-17", displayName: "Олена" },
  text: "Допомогла підставка з охолодженням", createdAt: ISODate("2026-09-01T08:20:00Z")
});
up("discussion_messages", "66f0c4000000000000000003", {
  discussionId: D1, parentId: ObjectId("66f0c4000000000000000001"), author: { userId: "u-33", displayName: "Максим" },
  text: "Оновіть BIOS", createdAt: ISODate("2026-09-03T11:00:00Z")
});
up("discussion_messages", "66f0c4000000000000000004", {
  discussionId: D2, parentId: null, author: { userId: "u-17", displayName: "Олена" },
  text: "Підключаються до iPhone без проблем", createdAt: ISODate("2026-09-06T12:00:00Z")
});

// product_ratings: порахувати з reviews (перерахунок ідемпотентний)
dbr.reviews.aggregate([
  { $group: { _id: "$productId", avg: { $avg: "$rating" }, count: { $sum: 1 },
              r1: { $sum: { $cond: [{ $eq: ["$rating", 1] }, 1, 0] } },
              r2: { $sum: { $cond: [{ $eq: ["$rating", 2] }, 1, 0] } },
              r3: { $sum: { $cond: [{ $eq: ["$rating", 3] }, 1, 0] } },
              r4: { $sum: { $cond: [{ $eq: ["$rating", 4] }, 1, 0] } },
              r5: { $sum: { $cond: [{ $eq: ["$rating", 5] }, 1, 0] } } } }
]).forEach(g => {
  dbr.product_ratings.updateOne(
    { productId: g._id },
    { $set: { avgRating: Math.round(g.avg * 100) / 100, count: I(g.count),
              distribution: { "1": I(g.r1), "2": I(g.r2), "3": I(g.r3), "4": I(g.r4), "5": I(g.r5) } } },
    { upsert: true });
});

print("ReviewsDb seeded. reviews=" + dbr.reviews.countDocuments() + ", discussions=" + dbr.discussions.countDocuments() + ", messages=" + dbr.discussion_messages.countDocuments() + ", ratings=" + dbr.product_ratings.countDocuments());
