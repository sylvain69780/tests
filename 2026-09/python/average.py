def calculate_average(numbers):
    # Start typing here and watch Copilot suggest the function body
    if not numbers:
        return 0
    return sum(numbers) / len(numbers)

print(calculate_average([1, 2, 3, 4, 5]))
