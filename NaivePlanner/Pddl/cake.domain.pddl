(define (domain CakeDomain)
    (:predicates
	(HaveCake ?x)
	(NotHaveCake ?x)
	(AteCake ?x)
	(NotAteCake ?x)
    )
    (:action EatCake
	:parameters (?x)
	:precondition (and (HaveCake ?x))
	:effect (and (not (HaveCake ?x)) (NotHaveCake ?x) (AteCake ?x) (not (NotAteCake ?x)))
	)
    (:action BakeCake
	:parameters (?x)
	:precondition (and (NotHaveCake ?x))
	:effect (and (not (NotHaveCake ?x))	(HaveCake ?x))
    )
)