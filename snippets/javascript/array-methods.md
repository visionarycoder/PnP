# Array Methods

**Description**: Modern JavaScript array manipulation patterns using ES6+ methods.

**Language/Technology**: JavaScript (ES6+)

**Code**:
```javascript
// 1. Filter and Map - Transform and filter arrays
const filterAndTransform = (items, predicate, transformer) => {
  return items.filter(predicate).map(transformer);
};

// Example
const numbers = [1, 2, 3, 4, 5, 6];
const evenSquares = filterAndTransform(
  numbers,
  n => n % 2 === 0,  // filter: keep even numbers
  n => n * n          // map: square them
);
// Result: [4, 16, 36]

// 2. Reduce - Sum, group, or aggregate data
const sum = arr => arr.reduce((acc, val) => acc + val, 0);

const groupBy = (arr, keyFn) => {
  return arr.reduce((groups, item) => {
    const key = keyFn(item);
    (groups[key] = groups[key] || []).push(item);
    return groups;
  }, {});
};

// Example
const total = sum([1, 2, 3, 4, 5]); // 15

const people = [
  { name: 'Alice', age: 25 },
  { name: 'Bob', age: 30 },
  { name: 'Charlie', age: 25 }
];
const byAge = groupBy(people, p => p.age);
// Result: { 25: [{name: 'Alice', age: 25}, {name: 'Charlie', age: 25}], 30: [{name: 'Bob', age: 30}] }

// 3. Find - Locate items
const findFirst = (arr, predicate) => arr.find(predicate);
const findAll = (arr, predicate) => arr.filter(predicate);
const findIndex = (arr, predicate) => arr.findIndex(predicate);

// Example
const users = [
  { id: 1, name: 'Alice', active: true },
  { id: 2, name: 'Bob', active: false },
  { id: 3, name: 'Charlie', active: true }
];

const firstActive = findFirst(users, u => u.active);
// Result: { id: 1, name: 'Alice', active: true }

const allActive = findAll(users, u => u.active);
// Result: [{ id: 1, name: 'Alice', active: true }, { id: 3, name: 'Charlie', active: true }]

// 4. Every and Some - Boolean checks
const allPositive = arr => arr.every(n => n > 0);
const hasNegative = arr => arr.some(n => n < 0);

// Example
console.log(allPositive([1, 2, 3]));    // true
console.log(allPositive([1, -2, 3]));   // false
console.log(hasNegative([1, 2, 3]));    // false
console.log(hasNegative([1, -2, 3]));   // true

// 5. Unique values
const unique = arr => [...new Set(arr)];
const uniqueBy = (arr, keyFn) => {
  const seen = new Map();
  return arr.filter(item => {
    const key = keyFn(item);
    if (seen.has(key)) return false;
    seen.set(key, true);
    return true;
  });
};

// Example
const uniqueNumbers = unique([1, 2, 2, 3, 3, 3, 4]);
// Result: [1, 2, 3, 4]

const uniqueUsers = uniqueBy(
  [{ id: 1, name: 'Alice' }, { id: 2, name: 'Bob' }, { id: 1, name: 'Alice' }],
  u => u.id
);
// Result: [{ id: 1, name: 'Alice' }, { id: 2, name: 'Bob' }]

// 6. Flatten arrays
const flatten = arr => arr.flat();
const flattenDeep = arr => arr.flat(Infinity);

// Example
const nested = [1, [2, 3], [4, [5, 6]]];
console.log(flatten(nested));      // [1, 2, 3, 4, [5, 6]]
console.log(flattenDeep(nested));  // [1, 2, 3, 4, 5, 6]

// 7. Chunk array
const chunk = (arr, size) => {
  return Array.from(
    { length: Math.ceil(arr.length / size) },
    (_, i) => arr.slice(i * size, i * size + size)
  );
};

// Example
const chunked = chunk([1, 2, 3, 4, 5, 6, 7], 3);
// Result: [[1, 2, 3], [4, 5, 6], [7]]

// 8. Sort arrays
const sortNumbers = arr => [...arr].sort((a, b) => a - b);
const sortBy = (arr, keyFn) => [...arr].sort((a, b) => {
  const aKey = keyFn(a);
  const bKey = keyFn(b);
  
  // Handle string comparison properly
  if (typeof aKey === 'string' && typeof bKey === 'string') {
    return aKey.localeCompare(bKey);
  }
  
  return aKey < bKey ? -1 : aKey > bKey ? 1 : 0;
});

// Example
const sorted = sortNumbers([3, 1, 4, 1, 5, 9, 2]);
// Result: [1, 1, 2, 3, 4, 5, 9]

const sortedByAge = sortBy(people, p => p.age);
// Result: sorted by age ascending
```

**Usage**:
```javascript
// Practical example: Process user data
const userData = [
  { id: 1, name: 'Alice', age: 25, active: true },
  { id: 2, name: 'Bob', age: 30, active: false },
  { id: 3, name: 'Charlie', age: 25, active: true },
  { id: 4, name: 'David', age: 35, active: true }
];

// Get active users, sorted by age
const activeUsersSorted = userData
  .filter(u => u.active)
  .sort((a, b) => a.age - b.age)
  .map(u => ({ name: u.name, age: u.age }));

console.log(activeUsersSorted);
// Result: [
//   { name: 'Alice', age: 25 },
//   { name: 'Charlie', age: 25 },
//   { name: 'David', age: 35 }
// ]

// Group by age and count
const ageCounts = userData.reduce((acc, user) => {
  acc[user.age] = (acc[user.age] || 0) + 1;
  return acc;
}, {});

console.log(ageCounts);
// Result: { 25: 2, 30: 1, 35: 1 }
```

**Notes**: 
- All methods are immutable (don't modify original arrays)
- Uses ES6+ features (arrow functions, spread operator, etc.)
- Works in modern browsers (Chrome 45+, Firefox 45+, Safari 10+, Edge 12+)
- For older browsers, use Babel or polyfills
- Methods like `flat()` require ES2019+ or polyfills
- Always create new arrays with spread operator when sorting to avoid mutation
- Related: [Object Methods](object-methods.md), [Functional Programming](functional-patterns.md)
