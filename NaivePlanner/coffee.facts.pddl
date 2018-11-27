(define (problem CoffeeProblem)
	(:domain CoffeeDoman)
	(:objects m)
	(:init
        (NeedsWater m)
        (NeedsCleanPod m)
        (NeedsEmptyCup m)
	)
	(:goal (and
        (HasServedCoffee m))
	)
)