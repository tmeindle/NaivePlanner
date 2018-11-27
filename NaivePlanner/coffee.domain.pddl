(define (domain CoffeeDoman)
    (:predicates
	(NeedsWater ?x)
	(NeedsCleanPod ?x)
	(NeedsEmptyCup ?x)
	(HasWater ?x)
	(HasCleanPod ?x)
	(HasEmptyCup ?x)
	(HasFullCup ?x)
	(HasServedCoffee ?x)
    )
    (:action LoadWater
	:parameters (?x)
	:precondition (and (NeedsWater ?x))
	:effect (and
		(not (NeedsWater ?x))
		(HasWater ?x) )
    )
    (:action LoadCleanPod
	:parameters (?x)
	:precondition (and (NeedsCleanPod ?x))
	:effect (and
		(not (NeedsCleanPod ?x))
		(HasCleanPod ?x) )
    )
    (:action LoadEmptyCup
	:parameters (?x)
	:precondition (and(NeedsEmptyCup ?x))
	:effect (and
		(not (NeedsEmptyCup ?x))
		(HasEmptyCup ?x) )
    )
    
    (:action BrewCoffee
	:parameters (?x)
	:precondition (and
			(HasWater ?x)
			(HasCleanPod ?x)
			(HasEmptyCup ?x) )	
	:effect (and
		(not (HasEmptyCup ?x))
		(HasFullCup ?x)
		(not (HasWater ?x))
		(NeedsWater ?x)
		(not (HasCleanPod ?x)) 
		(NeedsCleanPod ?x))
    )

    (:action ServeCoffee
	:parameters (?x)
	:precondition (and
		(HasFullCup ?x) )
	:effect (and
		(not (HasFullCup ?x))
		(HasServedCoffee ?x))
    )
)