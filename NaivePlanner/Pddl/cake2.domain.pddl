(define (domain CakeDomain)
    (:predicates (HC) (NotHC) (AC) (NotAC) )
    
	(:action EC
		:parameters ()
		:precondition (and (HC))
		:effect (and (not (HC)) (NotHC) (AC) (not (NotAC)))
	)
    (:action BC
		:parameters ()
		:precondition (and (NotHC))
		:effect (and (not (NotHC)) (HC))
    )
)