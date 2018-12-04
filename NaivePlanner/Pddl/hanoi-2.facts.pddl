(define (problem hanoi2)
  (:domain hanoi)
  (:objects p1 p2 p3 d1 d2)
  (:init 
   (smaller d1 p1) 
   (smaller d2 p1) 
   
   (smaller d1 p2) 
   (smaller d2 p2) 
   
   (smaller d1 p3) 
   (smaller d2 p3) 
   
   (smaller d1 d2)
   
   (clear p2) 
   (clear p3) 
   (clear d1)
   (on p1 d2) 
   (on d2 d1))
  (:goal (and 
   (clear p1) 
   (clear p2) 
   (clear d1)
   (on p3 d2) 
   (on d2 d1)
  )
)