(define (domain hanoi)
  (:predicates 
	(clear ?x) 
	(on ?x ?y) 
	(smaller ?x ?y))
  (:action move
    :parameters (?d ?from ?to)
	  :precondition (and 
		(smaller ?d ?to) 
		(on ?from ?d ) 
		(clear ?d) 
		(clear ?to))
    :effect  (and 
		(clear ?from) 
		(not (clear ?to))
		(on ?to ?d) 
		(not (on ?from ?d))  
		
		)
	)
)