class_name TestCase
extends Node
## Minimal assertions for tests/test_*.gd (run with `-- --test`).

var errors: Array[String] = []


func check(condition: bool, message: String) -> void:
	if not condition:
		errors.append(message)


func check_eq(actual: Variant, expected: Variant, message := "") -> void:
	if actual != expected:
		errors.append("%s expected %s, got %s" % [message, str(expected), str(actual)])


func check_near(actual: float, expected: float, message := "", tolerance := 0.001) -> void:
	if absf(actual - expected) > tolerance:
		errors.append("%s expected %.4f, got %.4f" % [message, expected, actual])
