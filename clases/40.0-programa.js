function quicksort(arr) {
  if (arr.length <= 1) return arr;

  const pivot = arr[0];
  const left = arr.slice(1).filter(x => x < pivot);
  const right = arr.slice(1).filter(x => x >= pivot);

  return [...quicksort(left), pivot, ...quicksort(right)];
}

console.log(quicksort([5, 3, 8, 1, 2, 7]));