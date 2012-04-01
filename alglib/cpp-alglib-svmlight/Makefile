readfile: dataanalysis.o
	g++ ap.o alglibinternal.o statistics.o specialfunctions.o linalg.o alglibmisc.o solvers.o optimization.o dataanalysis.o readsvm.cpp -o readfile

dataanalysis.o: ap.o alglibinternal.o statistics.o specialfunctions.o linalg.o alglibmisc.o solvers.o optimization.o dataanalysis.cpp
	g++ -c dataanalysis.cpp
	
statistics.o: statistics.cpp
	g++ -c statistics.cpp

specialfunctions.o: specialfunctions.cpp
	g++ -c specialfunctions.cpp

solvers.o: solvers.cpp
	g++ -c solvers.cpp

optimization.o: optimization.cpp
	g++ -c optimization.cpp

linalg.o: linalg.cpp
	g++ -c linalg.cpp

alglibinternal.o: alglibinternal.cpp
	g++ -c alglibinternal.cpp

ap.o: ap.cpp
	g++ -c ap.cpp

alglibmisc.o: alglibmisc.cpp
	g++ -c alglibmisc.cpp

clean:
	-rm -f *.o

default:
	g++ alglibinternal.cpp alglibmisc.cpp ap.cpp dataanalysis.cpp diffequations.cpp fasttransforms.cpp integration.cpp interpolation.cpp linalg.cpp optimization.cpp solvers.cpp specialfunctions.cpp statistics.cpp readsvm.cpp -o readsvm

all:
	g++ alglibinternal.cpp alglibmisc.cpp ap.cpp dataanalysis.cpp diffequations.cpp fasttransforms.cpp integration.cpp interpolation.cpp linalg.cpp optimization.cpp solvers.cpp specialfunctions.cpp statistics.cpp simpleforest.cpp -o simpleforest
  
testi:
	g++ alglibinternal.cpp alglibmisc.cpp ap.cpp dataanalysis.cpp diffequations.cpp fasttransforms.cpp integration.cpp interpolation.cpp linalg.cpp optimization.cpp solvers.cpp specialfunctions.cpp statistics.cpp test_i.cpp -o test_i

testc: 
	g++ alglibinternal.cpp alglibmisc.cpp ap.cpp dataanalysis.cpp diffequations.cpp fasttransforms.cpp integration.cpp interpolation.cpp linalg.cpp optimization.cpp solvers.cpp specialfunctions.cpp statistics.cpp test_c.cpp -o test_c

