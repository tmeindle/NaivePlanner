(define (domain blockworld)
  (:predicates 
    (ontable ?x) 
    (on ?x ?y) 
    (clear ?x)
  )
  
  (:action MoveToTable
    :parameters (?block ?from)
    :precondition (and (clear ?block) (on ?from ?block))
    :effect (and (clear ?from) (ontable ?block) (not (on ?from ?block))))

  (:action MoveToBlock
    :parameters (?block ?from ?to)
    :precondition (and (clear ?block) (clear ?to) (on ?from ?block))
    :effect (and (clear ?from) (on ?to ?block) (not (clear ?to)) (not (on ?from ?block))))

  (:action MoveFromTable
    :parameters (?block ?to)
    :precondition (and (clear ?block) (clear ?to) (ontable ?block))
    :effect (and (on ?to ?block) (not (clear ?to)) (not (ontable ?block))))
  )