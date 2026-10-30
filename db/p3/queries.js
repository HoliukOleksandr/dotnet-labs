// Приклади запитів та агрегацій. Запуск: mongosh "mongodb://localhost:27017" queries.js
const r = db.getSiblingDB("ReviewsDb");

print("--- 1. Документи з РІЗНИМИ наборами полів в одній колекції ---");
r.reviews.find({}, { productName: 1, rating: 1, "author.displayName": 1, "author.badge": 1, photos: 1, pros: 1, videoUrl: 1 }).forEach(printjson);

print("--- 2. Останні відгуки товару 1 (індекс productId+createdAt) ---");
r.reviews.find({ productId: 1 }).sort({ createdAt: -1 }).limit(5).forEach(printjson);

print("--- 3. Відгуки, де є поле photos ---");
r.reviews.find({ photos: { $exists: true } }, { productName: 1, photos: 1 }).forEach(printjson);

print("--- 4. Пошук у вкладених коментарях ---");
r.reviews.find({ "comments.author": "Андрій" }, { productName: 1, comments: 1 }).forEach(printjson);

print("--- 5. Повнотекстовий пошук ---");
r.reviews.find({ $text: { $search: "екран" } }, { productName: 1, text: 1 }).forEach(printjson);

print("--- 6. Hybrid: відгук + його обговорення через $lookup (reference) ---");
r.reviews.aggregate([
  { $match: { discussionId: { $exists: true } } },
  { $lookup: { from: "discussions", localField: "discussionId", foreignField: "_id", as: "discussion" } },
  { $unwind: "$discussion" },
  { $project: { productName: 1, rating: 1, "discussion.title": 1, "discussion.messageCount": 1 } }
]).forEach(printjson);

print("--- 7. Середній рейтинг і кількість відгуків по товарах ---");
r.reviews.aggregate([
  { $group: { _id: { id: "$productId", name: "$productName" }, avg: { $avg: "$rating" }, cnt: { $sum: 1 } } },
  { $sort: { avg: -1 } }
]).forEach(printjson);

print("--- 8. Пагінація повідомлень обговорення (індекс discussionId+createdAt) ---");
r.discussion_messages.find({ discussionId: ObjectId("66f0c3000000000000000001") })
  .sort({ createdAt: 1 }).skip(0).limit(10).forEach(printjson);

print("--- 9. Зведена статистика (product_ratings) ---");
r.product_ratings.find().forEach(printjson);

print("--- 10. Оновлення денормалізованого поля подією з Каталогу (імітація) ---");
r.reviews.updateMany({ productId: 5 }, { $set: { productName: "Навушники H9" } });
r.discussions.updateMany({ productId: 5 }, { $set: { productName: "Навушники H9" } });

print("--- 11. Перевірка валідації схеми: rating=7 має бути відхилено ---");
try {
  r.reviews.insertOne({ productId: NumberInt(1), productName: "x", author: { userId: "u", displayName: "d" },
                        rating: NumberInt(7), createdAt: new Date() });
  print("ПОМИЛКА: валідація не спрацювала");
} catch (e) { print("OK, відхилено: " + e.message); }
