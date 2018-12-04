(define (problem MonkeyProblem)
	(:domain MonkeyDomain)
	(:objects A B C)
	(:init
        (At A)(LevelLow)(BoxAt C)(BananasAt B)
	)
	(:goal (and
        (HasBananas))
	)
)