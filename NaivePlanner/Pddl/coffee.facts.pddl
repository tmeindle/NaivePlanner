﻿(define (problem CoffeeProblem)
	(:domain CoffeeDomain)
	(:objects m)
	(:init (NotHasWater m) (NotHasCleanPod m) (NotHasEmptyCup m) (NotHasFullCup m))
	(:goal (and (HasServedCoffee m)))
)