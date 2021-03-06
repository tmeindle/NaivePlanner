﻿(define (problem hanoi3)
  (:domain hanoi)
  (:objects p1 p2 p3  d1 d2 d3 d4)
  (:init 
   (smaller d1 p1) 
   (smaller d2 p1) 
   (smaller d3 p1)
   (smaller d4 p1)
   
   (smaller d1 p2) 
   (smaller d2 p2) 
   (smaller d3 p2)
   (smaller d4 p2)

   (smaller d1 p3) 
   (smaller d2 p3) 
   (smaller d3 p3)
   (smaller d4 p3)

   (smaller d1 d2)
   (smaller d1 d3)
   (smaller d1 d4)
   
   (smaller d2 d3)
   (smaller d2 d4)
   
   (smaller d3 d4)
   
   (clear p2) 
   (clear p3) 
   (clear d1)
   (on p1 d4) 
   (on d4 d3) 
   (on d3 d2) 
   (on d2 d1))
  (:goal 
	(and 
   (clear p1) 
   (clear p2) 
   (clear d1)
   (on p3 d4) 
   (on d4 d3) 
   (on d3 d2) 
   (on d2 d1))
  )
)
  