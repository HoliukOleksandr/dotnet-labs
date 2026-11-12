// ============================================================
// Проєкт №3: ReviewsDb (контекст «Відгуки та обговорення», MongoDB)
// collections.js: колекції, JSON Schema-валідація, індекси.
// Запуск: mongosh "mongodb://localhost:27017" collections.js
// Ідемпотентний: можна запускати повторно (collMod оновлює валідатор).
// ============================================================
const dbr = db.getSiblingDB("ReviewsDb");
const I = NumberInt;
const existing = dbr.getCollectionNames();

function ensureCollection(name, validator) {
  if (!existing.includes(name)) {
    dbr.createCollection(name, { validator: { $jsonSchema: validator }, validationLevel: "strict" });
  } else {
    dbr.runCommand({ collMod: name, validator: { $jsonSchema: validator } });
  }
}

// ---------- 1. reviews: embedding (author, comments) + reference (discussionId) + денормалізація (productName) ----------
ensureCollection("reviews", {
  bsonType: "object",
  required: ["productId", "productName", "author", "rating", "createdAt"],
  properties: {
    productId:   { bsonType: ["int", "long"], description: "посилання на CatalogDb.Products.Id (без FK)" },
    productName: { bsonType: "string",        description: "ДЕНОРМАЛІЗОВАНА копія для читання; оновлюється подіями з Каталогу" },
    author: {
      bsonType: "object", required: ["userId", "displayName"],
      properties: { userId: { bsonType: "string" }, displayName: { bsonType: "string" } }
    },
    rating: { bsonType: "int", minimum: 1, maximum: 5 },
    text:   { bsonType: "string" },
    photos: { bsonType: "array", items: { bsonType: "string" } },
    comments: {
      bsonType: "array",
      items: { bsonType: "object", required: ["author", "text", "createdAt"],
               properties: { author: { bsonType: "string" }, text: { bsonType: "string" }, createdAt: { bsonType: "date" } } }
    },
    discussionId: { bsonType: "objectId" },
    createdAt:    { bsonType: "date" }
  }
});

// ---------- 2. discussions: заголовок обговорення (reference на messages) ----------
ensureCollection("discussions", {
  bsonType: "object",
  required: ["productId", "title", "createdAt"],
  properties: {
    productId:     { bsonType: ["int", "long"] },
    productName:   { bsonType: "string" },
    title:         { bsonType: "string" },
    messageCount:  { bsonType: ["int", "long"], minimum: 0 },
    lastMessageAt: { bsonType: "date" },
    createdAt:     { bsonType: "date" }
  }
});

// ---------- 3. discussion_messages: НЕОБМЕЖЕНО велика підколекція -> reference, а не embed ----------
ensureCollection("discussion_messages", {
  bsonType: "object",
  required: ["discussionId", "author", "text", "createdAt"],
  properties: {
    discussionId: { bsonType: "objectId" },
    parentId:     { bsonType: ["objectId", "null"], description: "відповідь на інше повідомлення (гілки)" },
    author:       { bsonType: "object", required: ["userId", "displayName"] },
    text:         { bsonType: "string" },
    createdAt:    { bsonType: "date" }
  }
});

// ---------- 4. product_ratings: агрегована зведена статистика (гібрид: reference по productId + вбудований розподіл) ----------
ensureCollection("product_ratings", {
  bsonType: "object",
  required: ["productId", "avgRating", "count", "distribution"],
  properties: {
    productId:    { bsonType: ["int", "long"] },
    avgRating:    { bsonType: ["double", "int"], minimum: 0, maximum: 5 },
    count:        { bsonType: ["int", "long"], minimum: 0 },
    distribution: { bsonType: "object", description: "кількість оцінок по зірках: {\"1\":..,\"5\":..}" }
  }
});

// ===================== ІНДЕКСИ (кожен під запит) =====================
dbr.reviews.createIndex({ productId: 1, createdAt: -1 });          // «останні відгуки товару»
dbr.reviews.createIndex({ "author.userId": 1 });                    // «усі відгуки користувача»
dbr.reviews.createIndex({ productId: 1, rating: -1 });              // «найкращі/найгірші відгуки товару»
dbr.reviews.createIndex({ text: "text", productName: "text" });     // повнотекстовий пошук
dbr.discussions.createIndex({ productId: 1, lastMessageAt: -1 });   // «обговорення товару за активністю»
dbr.discussion_messages.createIndex({ discussionId: 1, createdAt: 1 }); // «повідомлення обговорення за часом (пагінація)»
dbr.product_ratings.createIndex({ productId: 1 }, { unique: true });    // одна зведена картка на товар

print("ReviewsDb collections ready: " + dbr.getCollectionNames().join(", "));
