(define (domain MonkeyDomain)
    (:predicates 
	(At ?x) 
	(LevelLow)	
	(LevelHigh)
	(BoxAt ?x)
	(BananasAt ?x)
	(HasBananas))
   
	(:action Move
	:parameters (?x ?y)
	:precondition (and (At ?x) (LevelLow))
	:effect (and (not (At ?x)) (At ?y))
    )

    (:action ClimbUp
	:parameters (?x)
	:precondition (and (At ?x) (BoxAt ?x) (LevelLow) )
	:effect (and (not (LevelLow)) (LevelHigh))
    )

	(:action ClimbDown
	:parameters (?x)
	:precondition (and (LevelHigh))
	:effect (and (not (LevelHigh)) (LevelLow))
    )
    
	(:action MoveBox
	:parameters (?x ?y)
	:precondition (and (At ?x) (BoxAt ?x))
	:effect (and (not (At ?x)) (not (BoxAt ?x)) (At ?y) (BoxAt ?y))
    )
    
    (:action TakeBananas
	:parameters (?x)
	:precondition (and (At ?x) (BoxAt ?x) (LevelHigh) (BananasAt ?x) )	
	:effect (and (not (BananasAt ?x)) (HasBananas))
    )
)