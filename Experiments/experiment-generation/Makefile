gesture: dataanalysis.o gesturetrain.cpp
	g++ -O3 -DNFEATURES=1000 -DNTRAINIMAGES=50 -DNTREES=2 ap.o alglibinternal.o statistics.o specialfunctions.o linalg.o alglibmisc.o solvers.o optimization.o dataanalysis.o gesturetrain.cpp -o experiment.1000.50.2.exe
	g++ -O3 -DNFEATURES=2000 -DNTRAINIMAGES=150 -DNTREES=5 ap.o alglibinternal.o statistics.o specialfunctions.o linalg.o alglibmisc.o solvers.o optimization.o dataanalysis.o gesturetrain.cpp -o experiment.2000.150.5.exe

dataanalysis.o: ap.o alglibinternal.o statistics.o specialfunctions.o linalg.o alglibmisc.o solvers.o optimization.o dataanalysis.cpp
	g++ -c -O3 dataanalysis.cpp
	
statistics.o: statistics.cpp
	g++ -c -O3 statistics.cpp

specialfunctions.o: specialfunctions.cpp
	g++ -c -O3 specialfunctions.cpp

solvers.o: solvers.cpp
	g++ -c -O3 solvers.cpp

optimization.o: optimization.cpp
	g++ -c -O3 optimization.cpp

linalg.o: linalg.cpp
	g++ -c -O3 linalg.cpp

alglibinternal.o: alglibinternal.cpp
	g++ -c -O3 alglibinternal.cpp

ap.o: ap.cpp
	g++ -c -O3 ap.cpp

alglibmisc.o: alglibmisc.cpp
	g++ -c -O3 alglibmisc.cpp

clean:
	-rm -f *.o

