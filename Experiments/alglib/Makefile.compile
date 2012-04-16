pruneaccuracy: dataanalysis.o pruneaccuracy.cpp
	g++ -g ap.o alglibinternal.o statistics.o specialfunctions.o linalg.o alglibmisc.o solvers.o optimization.o dataanalysis.o pruneaccuracy.cpp -o pruneaccuracy

trainingaccuracy: dataanalysis.o trainingaccuracy.cpp
	g++ -g ap.o alglibinternal.o statistics.o specialfunctions.o linalg.o alglibmisc.o solvers.o optimization.o dataanalysis.o trainingaccuracy.cpp -o trainingaccuracy

gesture: dataanalysis.o gesturetrain.cpp
	g++ -g ap.o alglibinternal.o statistics.o specialfunctions.o linalg.o alglibmisc.o solvers.o optimization.o dataanalysis.o gesturetrain.cpp -o gesturetrain

dataanalysis.o: ap.o alglibinternal.o statistics.o specialfunctions.o linalg.o alglibmisc.o solvers.o optimization.o dataanalysis.cpp
	g++ -c -g dataanalysis.cpp
	
statistics.o: statistics.cpp
	g++ -c -g statistics.cpp

specialfunctions.o: specialfunctions.cpp
	g++ -c -g specialfunctions.cpp

solvers.o: solvers.cpp
	g++ -c -g solvers.cpp

optimization.o: optimization.cpp
	g++ -c -g optimization.cpp

linalg.o: linalg.cpp
	g++ -c -g linalg.cpp

alglibinternal.o: alglibinternal.cpp
	g++ -c -g alglibinternal.cpp

ap.o: ap.cpp
	g++ -c -g ap.cpp

alglibmisc.o: alglibmisc.cpp
	g++ -c -g alglibmisc.cpp

clean:
	-rm -f *.o

