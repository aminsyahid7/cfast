FILTERC =
FILTERF =

VPATH = ../../source
bin = .

# Definition of the object variables

obj =  precision.o modules.o cnduct.o convec.o convecflow.o datacopy.o ddassl.o dfzero.o fireflow.o getdp.o getvals.o hvacflow.o initmm.o interp.o main.o readdata.o resid.o solve.o svout.o ventflow.o zonefire.o
objwin = $(obj:.o=.obj)

#*** General Purpose Rules ***

no_target:
	@echo \******** You did not specify a make target \********
	@echo Use one of targets found in the Makefile

.SUFFIXES: .f .F90 .c .obj .o .f90 .cpp

.c.obj:
	$(CC) -c $(CFLAGS) $(FILTERC) $(INC_DIR) $<
.c.o:
	$(CC) -c $(CFLAGS) $(FILTERC) $(INC_DIR) $<
.F90.o:
	$(FC) -c $(FFLAGS) $(FILTERF) $<
.F90.obj:
	$(FC) -c $(FFLAGS) $(FILTERF) $<
.f90.obj:
	$(FC) -c $(FFLAGS) $(FILTERF) $<
.f90.o:
	$(FC) -c $(FFLAGS) $(FILTERF) $<
.f.obj:
	$(FC) -c $(FFLAGS) $(FILTERF) $<
.f.o:
	$(FC) -c $(FFLAGS) $(FILTERF) $<


# ********  rules for each platform supported *************

# ------------- Win 32 ----------------
#  32 bit windows

intel_win_32 : CFLAGS    = -O1 /Qvec_report0 /nologo -D WIN32 -I $(SOURCE_DIR)/shared
intel_win_32 : FFLAGS    = 
intel_win_32 : CC        = icl
intel_win_32 : CPP       = icl
intel_win_32 : FC        = ifort
intel_win_32 : exe       = zfm_win_32.exe

intel_win_32 : $(objwin)
	$(FC) -o $(bin)/$(exe) $(objwin) 

# ------------- Linux 32 ----------------
#  32 bit linux

intel_linux_32 : FFLAGS    = 
intel_linux_32 : CFLAGS    = -O2 -m32 -vec_report0 
intel_linux_32 : CC        = icc
intel_linux_32 : CPP       = icc
intel_linux_32 : FC        = ifort
intel_linux_32 : exe       = zfm_linux_32

intel_linux_32 : $(obj)
	$(FC) -o $(bin)/$(exe) $(obj) 

# ------------- OSX 32 ----------------
#  32 bit OSX

intel_osx_32 : FFLAGS    = 
intel_osx_32 : CFLAGS    = -O2 -m32 -vec_report0 
intel_osx_32 : CC        = icc
intel_osx_32 : CPP       = icc
intel_osx_32 : FC        = ifort
intel_osx_32 : exe       = zfm_osx_32

intel_osx_32 : $(obj)
	$(FC) -o $(bin)/$(exe) $(obj) 

#-------------- Clean Target to remove Object and Module files -----------

.PHONY : clean
clean:
	rm *.o
