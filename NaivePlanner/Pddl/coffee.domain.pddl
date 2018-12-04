(define (domain CoffeeDomain)
    (:predicates
	(HasWater ?x)
	(NotHasWater ?x)
	(HasCleanPod ?x)
	(NotHasCleanPod ?x)
	(HasEmptyCup ?x)
	(NotHasEmptyCup ?x)
	(HasFullCup ?x)
	(NotHasFullCup ?x)
	(HasServedCoffee ?x)
    )

    (:action LoadWater
	:parameters (?x)
	:precondition (and (NotHasWater ?x))
	:effect (and
		(not (NotHasWater ?x))
		(HasWater ?x) )
    )
    (:action LoadCleanPod
	:parameters (?x)
	:precondition (and (NotHasCleanPod ?x))
	:effect (and
		(not (NotHasCleanPod ?x))
		(HasCleanPod ?x) )
    )
    (:action LoadEmptyCup
	:parameters (?x)
	:precondition (and (NotHasEmptyCup ?x) (NotHasFullCup ?x))
	:effect (and (not (NotHasEmptyCup ?x)) (HasEmptyCup ?x))
    )
    
    (:action BrewCoffee
	:parameters (?x)
	:precondition (and (HasWater ?x) (HasCleanPod ?x) (HasEmptyCup ?x) (NotHasFullCup ?x))	
	:effect (and (not (HasEmptyCup ?x)) (NotHasEmptyCup ?x)
				 (HasFullCup ?x) (not (NotHasFullCup ?x))
		         (not (HasWater ?x)) (NotHasWater ?x)
		         (not (HasCleanPod ?x)) (NotHasCleanPod ?x))
    )

    (:action ServeCoffee
	:parameters (?x)
	:precondition (and (HasFullCup ?x) (NotHasEmptyCup ?x))
	:effect (and (HasServedCoffee ?x) (not (HasFullCup ?x)) (NotHasFullCup ?x))
	)
)