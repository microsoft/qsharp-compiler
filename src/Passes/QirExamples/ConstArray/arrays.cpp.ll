; ModuleID = '/Users/tfr/Documents/Projects/qsharp-runtime/src/Qir/Runtime/lib/QIR/arrays.cpp'
source_filename = "/Users/tfr/Documents/Projects/qsharp-runtime/src/Qir/Runtime/lib/QIR/arrays.cpp"
target datalayout = "e-m:o-p270:32:32-p271:32:32-p272:64:64-i64:64-f80:128-n8:16:32:64-S128"
target triple = "x86_64-apple-macosx11.0.0"

%Array = type <{ i32, i32, i8, [7 x i8], %"class.std::__1::vector", i8*, i8, [3 x i8], i32, i32, [4 x i8] }>
%"class.std::__1::vector" = type { %"class.std::__1::__vector_base" }
%"class.std::__1::__vector_base" = type { i32*, i32*, %"class.std::__1::__compressed_pair" }
%"class.std::__1::__compressed_pair" = type { %"struct.std::__1::__compressed_pair_elem" }
%"struct.std::__1::__compressed_pair_elem" = type { i32* }
%"class.std::__1::unique_ptr" = type { %"class.std::__1::__compressed_pair.1" }
%"class.std::__1::__compressed_pair.1" = type { %"struct.std::__1::__compressed_pair_elem.2" }
%"struct.std::__1::__compressed_pair_elem.2" = type { %"struct.Microsoft::Quantum::QirExecutionContext"* }
%"struct.Microsoft::Quantum::QirExecutionContext" = type { %"struct.Microsoft::Quantum::IRuntimeDriver"*, i8, %"class.std::__1::unique_ptr.3" }
%"struct.Microsoft::Quantum::IRuntimeDriver" = type opaque
%"class.std::__1::unique_ptr.3" = type { %"class.std::__1::__compressed_pair.4" }
%"class.std::__1::__compressed_pair.4" = type { %"struct.std::__1::__compressed_pair_elem.5" }
%"struct.std::__1::__compressed_pair_elem.5" = type { %"struct.Microsoft::Quantum::AllocationsTracker"* }
%"struct.Microsoft::Quantum::AllocationsTracker" = type opaque
%class.QUBIT = type opaque
%struct.QirString = type { i64, %"class.std::__1::basic_string" }
%"class.std::__1::basic_string" = type { %"class.std::__1::__compressed_pair.10" }
%"class.std::__1::__compressed_pair.10" = type { %"struct.std::__1::__compressed_pair_elem.11" }
%"struct.std::__1::__compressed_pair_elem.11" = type { %"struct.std::__1::basic_string<char, std::__1::char_traits<char>, std::__1::allocator<char>>::__rep" }
%"struct.std::__1::basic_string<char, std::__1::char_traits<char>, std::__1::allocator<char>>::__rep" = type { %union.anon }
%union.anon = type { %"struct.std::__1::basic_string<char, std::__1::char_traits<char>, std::__1::allocator<char>>::__long" }
%"struct.std::__1::basic_string<char, std::__1::char_traits<char>, std::__1::allocator<char>>::__long" = type { i64, i64, i8* }
%struct.__va_list_tag = type { i32, i32, i8*, i8* }
%"class.std::__1::allocator" = type { i8 }
%"struct.std::__1::__split_buffer" = type { i32*, i32*, i32*, %"class.std::__1::__compressed_pair.17" }
%"class.std::__1::__compressed_pair.17" = type { %"struct.std::__1::__compressed_pair_elem", %"struct.std::__1::__compressed_pair_elem.18" }
%"struct.std::__1::__compressed_pair_elem.18" = type { %"class.std::__1::allocator"* }
%struct.QirRange = type { i64, i64, i64 }
%struct.CheckedRange = type { i64, i64, i64, i64 }
%"class.std::__1::__wrap_iter" = type { i32* }
%"struct.std::__1::multiplies" = type { i8 }
%"class.std::__1::__wrap_iter.16" = type { i32* }
%"class.std::runtime_error" = type { %"class.std::exception", %"class.std::__1::__libcpp_refstring" }
%"class.std::exception" = type { i32 (...)** }
%"class.std::__1::__libcpp_refstring" = type { i8* }
%"struct.std::__1::__default_init_tag" = type { i8 }
%"class.std::__1::__vector_base_common" = type { i8 }
%"struct.std::__1::__compressed_pair_elem.0" = type { i8 }
%"struct.std::__1::integral_constant" = type { i8 }
%"struct.std::__1::__has_destroy" = type { i8 }
%"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction" = type { %"class.std::__1::vector"*, i32*, i32* }
%"struct.std::__1::__has_construct" = type { i8 }
%"struct.std::__1::__less" = type { i8 }
%"struct.std::__1::__has_max_size" = type { i8 }
%"class.std::__1::__split_buffer_common" = type { i8 }
%"class.std::length_error" = type { %"class.std::logic_error" }
%"class.std::logic_error" = type { %"class.std::exception", %"class.std::__1::__libcpp_refstring" }
%"struct.std::__1::integral_constant.19" = type { i8 }
%"struct.std::__1::__has_select_on_container_copy_construction" = type { i8 }
%"struct.std::__1::__has_construct.20" = type { i8 }

@.str = private unnamed_addr constant [33 x i8] c"Cannot resurrect released array!\00", align 1
@__func__._ZN8QirArray6AddRefEv = private unnamed_addr constant [7 x i8] c"AddRef\00", align 1
@.str.1 = private unnamed_addr constant [80 x i8] c"/Users/tfr/Documents/Projects/qsharp-runtime/src/Qir/Runtime/lib/QIR/arrays.cpp\00", align 1
@.str.2 = private unnamed_addr constant [58 x i8] c"this->refCount != 0 && \22Cannot resurrect released array!\22\00", align 1
@.str.3 = private unnamed_addr constant [39 x i8] c"Cannot release already released array!\00", align 1
@__func__._ZN8QirArray7ReleaseEv = private unnamed_addr constant [8 x i8] c"Release\00", align 1
@.str.4 = private unnamed_addr constant [64 x i8] c"this->refCount != 0 && \22Cannot release already released array!\22\00", align 1
@__func__._ZN8QirArrayC2EjjhONSt3__16vectorIjNS0_9allocatorIjEEEE = private unnamed_addr constant [9 x i8] c"QirArray\00", align 1
@.str.5 = private unnamed_addr constant [19 x i8] c"itemSizeBytes != 0\00", align 1
@.str.6 = private unnamed_addr constant [13 x i8] c"dimCount > 0\00", align 1
@.str.7 = private unnamed_addr constant [71 x i8] c"this->dimensionSizes.empty() || this->dimensionSizes[0] == this->count\00", align 1
@.str.8 = private unnamed_addr constant [79 x i8] c"this->count * (TBufSize)itemSizeInBytes < std::numeric_limits<TBufSize>::max()\00", align 1
@.str.9 = private unnamed_addr constant [49 x i8] c"bufferSize <= std::numeric_limits<size_t>::max()\00", align 1
@.str.10 = private unnamed_addr constant [87 x i8] c"(TBufSize)(this->count) * this->itemSizeInBytes < std::numeric_limits<TBufSize>::max()\00", align 1
@.str.11 = private unnamed_addr constant [43 x i8] c"size <= std::numeric_limits<size_t>::max()\00", align 1
@__func__._ZN8QirArrayD2Ev = private unnamed_addr constant [10 x i8] c"~QirArray\00", align 1
@.str.12 = private unnamed_addr constant [24 x i8] c"this->buffer == nullptr\00", align 1
@__func__._ZNK8QirArray14GetItemPointerEj = private unnamed_addr constant [15 x i8] c"GetItemPointer\00", align 1
@.str.13 = private unnamed_addr constant [20 x i8] c"index < this->count\00", align 1
@__func__._ZN8QirArray6AppendEPKS_ = private unnamed_addr constant [7 x i8] c"Append\00", align 1
@.str.14 = private unnamed_addr constant [18 x i8] c"!this->ownsQubits\00", align 1
@.str.15 = private unnamed_addr constant [48 x i8] c"this->itemSizeInBytes == other->itemSizeInBytes\00", align 1
@.str.16 = private unnamed_addr constant [48 x i8] c"this->dimensions == 1 && other->dimensions == 1\00", align 1
@.str.17 = private unnamed_addr constant [89 x i8] c"(TBufSize)(other->count) * other->itemSizeInBytes < std::numeric_limits<TBufSize>::max()\00", align 1
@__func__.__quantum__rt__qubit_release_array = private unnamed_addr constant [35 x i8] c"__quantum__rt__qubit_release_array\00", align 1
@.str.18 = private unnamed_addr constant [15 x i8] c"qa->ownsQubits\00", align 1
@__func__.__quantum__rt__array_create_1d = private unnamed_addr constant [31 x i8] c"__quantum__rt__array_create_1d\00", align 1
@.str.19 = private unnamed_addr constant [20 x i8] c"itemSizeInBytes > 0\00", align 1
@.str.20 = private unnamed_addr constant [52 x i8] c"Attempting to decrement reference count below zero!\00", align 1
@__func__.__quantum__rt__array_update_reference_count = private unnamed_addr constant [44 x i8] c"__quantum__rt__array_update_reference_count\00", align 1
@.str.21 = private unnamed_addr constant [65 x i8] c"i == -1 && \22Attempting to decrement reference count below zero!\22\00", align 1
@.str.22 = private unnamed_addr constant [32 x i8] c"Alias count cannot be negative!\00", align 1
@__func__.__quantum__rt__array_get_element_ptr_1d = private unnamed_addr constant [40 x i8] c"__quantum__rt__array_get_element_ptr_1d\00", align 1
@.str.23 = private unnamed_addr constant [17 x i8] c"array != nullptr\00", align 1
@__func__.__quantum__rt__array_get_dim = private unnamed_addr constant [29 x i8] c"__quantum__rt__array_get_dim\00", align 1
@__func__.__quantum__rt__array_get_size = private unnamed_addr constant [30 x i8] c"__quantum__rt__array_get_size\00", align 1
@.str.24 = private unnamed_addr constant [24 x i8] c"dim < array->dimensions\00", align 1
@__func__.__quantum__rt__array_concatenate = private unnamed_addr constant [33 x i8] c"__quantum__rt__array_concatenate\00", align 1
@.str.25 = private unnamed_addr constant [35 x i8] c"head != nullptr && tail != nullptr\00", align 1
@.str.26 = private unnamed_addr constant [47 x i8] c"head->dimensions == 1 && tail->dimensions == 1\00", align 1
@__func__.__quantum__rt__array_create_nonvariadic = private unnamed_addr constant [40 x i8] c"__quantum__rt__array_create_nonvariadic\00", align 1
@.str.27 = private unnamed_addr constant [66 x i8] c"countDimensions < std::numeric_limits<QirArray::TDimCount>::max()\00", align 1
@__func__.__quantum__rt__array_get_element_ptr_nonvariadic = private unnamed_addr constant [49 x i8] c"__quantum__rt__array_get_element_ptr_nonvariadic\00", align 1
@.str.28 = private unnamed_addr constant [42 x i8] c"indexes.back() < array->dimensionSizes[i]\00", align 1
@__func__.__quantum__rt__array_get_element_ptr = private unnamed_addr constant [37 x i8] c"__quantum__rt__array_get_element_ptr\00", align 1
@__func__.quantum__rt__array_slice = private unnamed_addr constant [25 x i8] c"quantum__rt__array_slice\00", align 1
@.str.29 = private unnamed_addr constant [36 x i8] c"dim >= 0 && dim < array->dimensions\00", align 1
@.str.30 = private unnamed_addr constant [101 x i8] c"(QirArray::TBufSize)rangeRunCount * itemSizeInBytes < std::numeric_limits<QirArray::TBufSize>::max()\00", align 1
@.str.31 = private unnamed_addr constant [19 x i8] c"dst < slice->count\00", align 1
@.str.32 = private unnamed_addr constant [107 x i8] c"(QirArray::TBufSize)singleIndexRunCount * itemSizeInBytes < std::numeric_limits<QirArray::TBufSize>::max()\00", align 1
@.str.33 = private unnamed_addr constant [79 x i8] c"(dst * itemSizeInBytes + chunkSize) <= (slice->count * slice->itemSizeInBytes)\00", align 1
@.str.34 = private unnamed_addr constant [102 x i8] c"(srcInner * (int64_t)itemSizeInBytes + (int64_t)chunkSize) <= (array->count * array->itemSizeInBytes)\00", align 1
@.str.35 = private unnamed_addr constant [14 x i8] c"srcInner >= 0\00", align 1
@__func__.__quantum__rt__array_project = private unnamed_addr constant [29 x i8] c"__quantum__rt__array_project\00", align 1
@.str.36 = private unnamed_addr constant [22 x i8] c"array->dimensions > 1\00", align 1
@.str.37 = private unnamed_addr constant [57 x i8] c"index >= 0 && index < array->dimensionSizes[(size_t)dim]\00", align 1
@.str.38 = private unnamed_addr constant [21 x i8] c"dst < project->count\00", align 1
@.str.39 = private unnamed_addr constant [14 x i8] c"invalid range\00", align 1
@_ZTISt13runtime_error = external constant i8*
@__func__._ZN12CheckedRangeC2ERK8QirRangex = private unnamed_addr constant [13 x i8] c"CheckedRange\00", align 1
@.str.40 = private unnamed_addr constant [16 x i8] c"this->width > 0\00", align 1
@.str.41 = private unnamed_addr constant [20 x i8] c"range out of bounds\00", align 1
@__func__._ZL8RunCountRKNSt3__16vectorIjNS_9allocatorIjEEEEh = private unnamed_addr constant [9 x i8] c"RunCount\00", align 1
@.str.42 = private unnamed_addr constant [64 x i8] c"(0 <= dimension) && ((size_t)dimension < dimensionSizes.size())\00", align 1
@.str.43 = private unnamed_addr constant [68 x i8] c"allocator<T>::allocate(size_t n) 'n' exceeds maximum supported size\00", align 1
@_ZTISt12length_error = external constant i8*
@_ZTVSt12length_error = external unnamed_addr constant { [5 x i8*] }, align 8

; Function Attrs: noinline optnone ssp uwtable
define i32 @_ZN8QirArray6AddRefEv(%Array* %0) #0 align 2 {
  %2 = alloca %Array*, align 8
  store %Array* %0, %Array** %2, align 8
  %3 = load %Array*, %Array** %2, align 8
  %4 = call nonnull align 8 dereferenceable(8) %"class.std::__1::unique_ptr"* @_ZN9Microsoft7Quantum13GlobalContextEv()
  %5 = call zeroext i1 @_ZNSt3__1neIN9Microsoft7Quantum19QirExecutionContextENS_14default_deleteIS3_EEEEbRKNS_10unique_ptrIT_T0_EEDn(%"class.std::__1::unique_ptr"* nonnull align 8 dereferenceable(8) %4, i8* null) #11
  br i1 %5, label %6, label %10

6:                                                ; preds = %1
  %7 = call nonnull align 8 dereferenceable(8) %"class.std::__1::unique_ptr"* @_ZN9Microsoft7Quantum13GlobalContextEv()
  %8 = call %"struct.Microsoft::Quantum::QirExecutionContext"* @_ZNKSt3__110unique_ptrIN9Microsoft7Quantum19QirExecutionContextENS_14default_deleteIS3_EEEptEv(%"class.std::__1::unique_ptr"* %7) #11
  %9 = bitcast %Array* %3 to i8*
  call void @_ZN9Microsoft7Quantum19QirExecutionContext8OnAddRefEPv(%"struct.Microsoft::Quantum::QirExecutionContext"* %8, i8* %9)
  br label %10

10:                                               ; preds = %6, %1
  %11 = getelementptr inbounds %Array, %Array* %3, i32 0, i32 8
  %12 = load i32, i32* %11, align 4
  %13 = icmp ne i32 %12, 0
  br i1 %13, label %14, label %15

14:                                               ; preds = %10
  br label %15

15:                                               ; preds = %14, %10
  %16 = phi i1 [ false, %10 ], [ true, %14 ]
  %17 = xor i1 %16, true
  br i1 %17, label %18, label %20

18:                                               ; preds = %15
  call void @__assert_rtn(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @__func__._ZN8QirArray6AddRefEv, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 26, i8* getelementptr inbounds ([58 x i8], [58 x i8]* @.str.2, i64 0, i64 0)) #14
  unreachable

19:                                               ; No predecessors!
  br label %21

20:                                               ; preds = %15
  br label %21

21:                                               ; preds = %20, %19
  %22 = getelementptr inbounds %Array, %Array* %3, i32 0, i32 8
  %23 = load i32, i32* %22, align 4
  %24 = add nsw i32 %23, 1
  store i32 %24, i32* %22, align 4
  ret i32 %24
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden zeroext i1 @_ZNSt3__1neIN9Microsoft7Quantum19QirExecutionContextENS_14default_deleteIS3_EEEEbRKNS_10unique_ptrIT_T0_EEDn(%"class.std::__1::unique_ptr"* nonnull align 8 dereferenceable(8) %0, i8* %1) #1 {
  %3 = alloca %"class.std::__1::unique_ptr"*, align 8
  %4 = alloca i8*, align 8
  store %"class.std::__1::unique_ptr"* %0, %"class.std::__1::unique_ptr"** %3, align 8
  store i8* %1, i8** %4, align 8
  %5 = load %"class.std::__1::unique_ptr"*, %"class.std::__1::unique_ptr"** %3, align 8
  %6 = call zeroext i1 @_ZNKSt3__110unique_ptrIN9Microsoft7Quantum19QirExecutionContextENS_14default_deleteIS3_EEEcvbEv(%"class.std::__1::unique_ptr"* %5) #11
  ret i1 %6
}

declare nonnull align 8 dereferenceable(8) %"class.std::__1::unique_ptr"* @_ZN9Microsoft7Quantum13GlobalContextEv() #2

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden %"struct.Microsoft::Quantum::QirExecutionContext"* @_ZNKSt3__110unique_ptrIN9Microsoft7Quantum19QirExecutionContextENS_14default_deleteIS3_EEEptEv(%"class.std::__1::unique_ptr"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::unique_ptr"*, align 8
  store %"class.std::__1::unique_ptr"* %0, %"class.std::__1::unique_ptr"** %2, align 8
  %3 = load %"class.std::__1::unique_ptr"*, %"class.std::__1::unique_ptr"** %2, align 8
  %4 = getelementptr inbounds %"class.std::__1::unique_ptr", %"class.std::__1::unique_ptr"* %3, i32 0, i32 0
  %5 = call nonnull align 8 dereferenceable(8) %"struct.Microsoft::Quantum::QirExecutionContext"** @_ZNKSt3__117__compressed_pairIPN9Microsoft7Quantum19QirExecutionContextENS_14default_deleteIS3_EEE5firstEv(%"class.std::__1::__compressed_pair.1"* %4) #11
  %6 = load %"struct.Microsoft::Quantum::QirExecutionContext"*, %"struct.Microsoft::Quantum::QirExecutionContext"** %5, align 8
  ret %"struct.Microsoft::Quantum::QirExecutionContext"* %6
}

declare void @_ZN9Microsoft7Quantum19QirExecutionContext8OnAddRefEPv(%"struct.Microsoft::Quantum::QirExecutionContext"*, i8*) #2

; Function Attrs: cold noreturn
declare void @__assert_rtn(i8*, i8*, i32, i8*) #3

; Function Attrs: noinline optnone ssp uwtable
define i32 @_ZN8QirArray7ReleaseEv(%Array* %0) #0 align 2 {
  %2 = alloca %Array*, align 8
  %3 = alloca i32, align 4
  store %Array* %0, %Array** %2, align 8
  %4 = load %Array*, %Array** %2, align 8
  %5 = call nonnull align 8 dereferenceable(8) %"class.std::__1::unique_ptr"* @_ZN9Microsoft7Quantum13GlobalContextEv()
  %6 = call zeroext i1 @_ZNSt3__1neIN9Microsoft7Quantum19QirExecutionContextENS_14default_deleteIS3_EEEEbRKNS_10unique_ptrIT_T0_EEDn(%"class.std::__1::unique_ptr"* nonnull align 8 dereferenceable(8) %5, i8* null) #11
  br i1 %6, label %7, label %11

7:                                                ; preds = %1
  %8 = call nonnull align 8 dereferenceable(8) %"class.std::__1::unique_ptr"* @_ZN9Microsoft7Quantum13GlobalContextEv()
  %9 = call %"struct.Microsoft::Quantum::QirExecutionContext"* @_ZNKSt3__110unique_ptrIN9Microsoft7Quantum19QirExecutionContextENS_14default_deleteIS3_EEEptEv(%"class.std::__1::unique_ptr"* %8) #11
  %10 = bitcast %Array* %4 to i8*
  call void @_ZN9Microsoft7Quantum19QirExecutionContext9OnReleaseEPv(%"struct.Microsoft::Quantum::QirExecutionContext"* %9, i8* %10)
  br label %11

11:                                               ; preds = %7, %1
  %12 = getelementptr inbounds %Array, %Array* %4, i32 0, i32 8
  %13 = load i32, i32* %12, align 4
  %14 = icmp ne i32 %13, 0
  br i1 %14, label %15, label %16

15:                                               ; preds = %11
  br label %16

16:                                               ; preds = %15, %11
  %17 = phi i1 [ false, %11 ], [ true, %15 ]
  %18 = xor i1 %17, true
  br i1 %18, label %19, label %21

19:                                               ; preds = %16
  call void @__assert_rtn(i8* getelementptr inbounds ([8 x i8], [8 x i8]* @__func__._ZN8QirArray7ReleaseEv, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 40, i8* getelementptr inbounds ([64 x i8], [64 x i8]* @.str.4, i64 0, i64 0)) #14
  unreachable

20:                                               ; No predecessors!
  br label %22

21:                                               ; preds = %16
  br label %22

22:                                               ; preds = %21, %20
  %23 = getelementptr inbounds %Array, %Array* %4, i32 0, i32 8
  %24 = load i32, i32* %23, align 4
  %25 = add nsw i32 %24, -1
  store i32 %25, i32* %23, align 4
  store i32 %25, i32* %3, align 4
  %26 = load i32, i32* %3, align 4
  %27 = icmp eq i32 %26, 0
  br i1 %27, label %28, label %48

28:                                               ; preds = %22
  %29 = getelementptr inbounds %Array, %Array* %4, i32 0, i32 6
  %30 = load i8, i8* %29, align 8
  %31 = trunc i8 %30 to i1
  br i1 %31, label %32, label %40

32:                                               ; preds = %28
  %33 = getelementptr inbounds %Array, %Array* %4, i32 0, i32 5
  %34 = load i8*, i8** %33, align 8
  %35 = bitcast i8* %34 to i64*
  %36 = icmp eq i64* %35, null
  br i1 %36, label %39, label %37

37:                                               ; preds = %32
  %38 = bitcast i64* %35 to i8*
  call void @_ZdaPv(i8* %38) #15
  br label %39

39:                                               ; preds = %37, %32
  br label %46

40:                                               ; preds = %28
  %41 = getelementptr inbounds %Array, %Array* %4, i32 0, i32 5
  %42 = load i8*, i8** %41, align 8
  %43 = icmp eq i8* %42, null
  br i1 %43, label %45, label %44

44:                                               ; preds = %40
  call void @_ZdaPv(i8* %42) #15
  br label %45

45:                                               ; preds = %44, %40
  br label %46

46:                                               ; preds = %45, %39
  %47 = getelementptr inbounds %Array, %Array* %4, i32 0, i32 5
  store i8* null, i8** %47, align 8
  br label %48

48:                                               ; preds = %46, %22
  %49 = load i32, i32* %3, align 4
  ret i32 %49
}

declare void @_ZN9Microsoft7Quantum19QirExecutionContext9OnReleaseEPv(%"struct.Microsoft::Quantum::QirExecutionContext"*, i8*) #2

; Function Attrs: nobuiltin nounwind
declare void @_ZdaPv(i8*) #4

; Function Attrs: noinline optnone ssp uwtable
define void @_ZN8QirArrayC2Ej(%Array* %0, i32 %1) unnamed_addr #0 align 2 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %3 = alloca %Array*, align 8
  %4 = alloca i32, align 4
  %5 = alloca i64*, align 8
  %6 = alloca i8*, align 8
  %7 = alloca i32, align 4
  %8 = alloca i32, align 4
  store %Array* %0, %Array** %3, align 8
  store i32 %1, i32* %4, align 4
  %9 = load %Array*, %Array** %3, align 8
  %10 = getelementptr inbounds %Array, %Array* %9, i32 0, i32 0
  %11 = load i32, i32* %4, align 4
  store i32 %11, i32* %10, align 8
  %12 = getelementptr inbounds %Array, %Array* %9, i32 0, i32 1
  store i32 8, i32* %12, align 4
  %13 = getelementptr inbounds %Array, %Array* %9, i32 0, i32 2
  store i8 1, i8* %13, align 8
  %14 = getelementptr inbounds %Array, %Array* %9, i32 0, i32 4
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEEC1Ev(%"class.std::__1::vector"* %14) #11
  %15 = getelementptr inbounds %Array, %Array* %9, i32 0, i32 5
  store i8* null, i8** %15, align 8
  %16 = getelementptr inbounds %Array, %Array* %9, i32 0, i32 6
  store i8 1, i8* %16, align 8
  %17 = getelementptr inbounds %Array, %Array* %9, i32 0, i32 8
  store i32 1, i32* %17, align 4
  %18 = getelementptr inbounds %Array, %Array* %9, i32 0, i32 9
  store i32 0, i32* %18, align 8
  %19 = getelementptr inbounds %Array, %Array* %9, i32 0, i32 0
  %20 = load i32, i32* %19, align 8
  %21 = icmp ugt i32 %20, 0
  br i1 %21, label %22, label %57

22:                                               ; preds = %2
  %23 = getelementptr inbounds %Array, %Array* %9, i32 0, i32 0
  %24 = load i32, i32* %23, align 8
  %25 = zext i32 %24 to i64
  %26 = call { i64, i1 } @llvm.umul.with.overflow.i64(i64 %25, i64 8)
  %27 = extractvalue { i64, i1 } %26, 1
  %28 = extractvalue { i64, i1 } %26, 0
  %29 = select i1 %27, i64 -1, i64 %28
  %30 = invoke noalias nonnull i8* @_Znam(i64 %29) #16
          to label %31 unwind label %49

31:                                               ; preds = %22
  %32 = bitcast i8* %30 to i64*
  store i64* %32, i64** %5, align 8
  store i32 0, i32* %8, align 4
  br label %33

33:                                               ; preds = %46, %31
  %34 = load i32, i32* %8, align 4
  %35 = getelementptr inbounds %Array, %Array* %9, i32 0, i32 0
  %36 = load i32, i32* %35, align 8
  %37 = icmp ult i32 %34, %36
  br i1 %37, label %38, label %53

38:                                               ; preds = %33
  %39 = invoke %class.QUBIT* @__quantum__rt__qubit_allocate()
          to label %40 unwind label %49

40:                                               ; preds = %38
  %41 = ptrtoint %class.QUBIT* %39 to i64
  %42 = load i64*, i64** %5, align 8
  %43 = load i32, i32* %8, align 4
  %44 = zext i32 %43 to i64
  %45 = getelementptr inbounds i64, i64* %42, i64 %44
  store i64 %41, i64* %45, align 8
  br label %46

46:                                               ; preds = %40
  %47 = load i32, i32* %8, align 4
  %48 = add i32 %47, 1
  store i32 %48, i32* %8, align 4
  br label %33

49:                                               ; preds = %68, %66, %62, %59, %38, %22
  %50 = landingpad { i8*, i32 }
          cleanup
  %51 = extractvalue { i8*, i32 } %50, 0
  store i8* %51, i8** %6, align 8
  %52 = extractvalue { i8*, i32 } %50, 1
  store i32 %52, i32* %7, align 4
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEED1Ev(%"class.std::__1::vector"* %14) #11
  br label %73

53:                                               ; preds = %33
  %54 = load i64*, i64** %5, align 8
  %55 = bitcast i64* %54 to i8*
  %56 = getelementptr inbounds %Array, %Array* %9, i32 0, i32 5
  store i8* %55, i8** %56, align 8
  br label %59

57:                                               ; preds = %2
  %58 = getelementptr inbounds %Array, %Array* %9, i32 0, i32 5
  store i8* null, i8** %58, align 8
  br label %59

59:                                               ; preds = %57, %53
  %60 = getelementptr inbounds %Array, %Array* %9, i32 0, i32 4
  %61 = getelementptr inbounds %Array, %Array* %9, i32 0, i32 0
  invoke void @_ZNSt3__16vectorIjNS_9allocatorIjEEE9push_backERKj(%"class.std::__1::vector"* %60, i32* nonnull align 4 dereferenceable(4) %61)
          to label %62 unwind label %49

62:                                               ; preds = %59
  %63 = invoke nonnull align 8 dereferenceable(8) %"class.std::__1::unique_ptr"* @_ZN9Microsoft7Quantum13GlobalContextEv()
          to label %64 unwind label %49

64:                                               ; preds = %62
  %65 = call zeroext i1 @_ZNSt3__1neIN9Microsoft7Quantum19QirExecutionContextENS_14default_deleteIS3_EEEEbRKNS_10unique_ptrIT_T0_EEDn(%"class.std::__1::unique_ptr"* nonnull align 8 dereferenceable(8) %63, i8* null) #11
  br i1 %65, label %66, label %72

66:                                               ; preds = %64
  %67 = invoke nonnull align 8 dereferenceable(8) %"class.std::__1::unique_ptr"* @_ZN9Microsoft7Quantum13GlobalContextEv()
          to label %68 unwind label %49

68:                                               ; preds = %66
  %69 = call %"struct.Microsoft::Quantum::QirExecutionContext"* @_ZNKSt3__110unique_ptrIN9Microsoft7Quantum19QirExecutionContextENS_14default_deleteIS3_EEEptEv(%"class.std::__1::unique_ptr"* %67) #11
  %70 = bitcast %Array* %9 to i8*
  invoke void @_ZN9Microsoft7Quantum19QirExecutionContext10OnAllocateEPv(%"struct.Microsoft::Quantum::QirExecutionContext"* %69, i8* %70)
          to label %71 unwind label %49

71:                                               ; preds = %68
  br label %72

72:                                               ; preds = %71, %64
  ret void

73:                                               ; preds = %49
  %74 = load i8*, i8** %6, align 8
  %75 = load i32, i32* %7, align 4
  %76 = insertvalue { i8*, i32 } undef, i8* %74, 0
  %77 = insertvalue { i8*, i32 } %76, i32 %75, 1
  resume { i8*, i32 } %77
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__16vectorIjNS_9allocatorIjEEEC1Ev(%"class.std::__1::vector"* %0) unnamed_addr #1 align 2 {
  %2 = alloca %"class.std::__1::vector"*, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %2, align 8
  %3 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %2, align 8
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEEC2Ev(%"class.std::__1::vector"* %3) #11
  ret void
}

; Function Attrs: nounwind readnone speculatable willreturn
declare { i64, i1 } @llvm.umul.with.overflow.i64(i64, i64) #5

; Function Attrs: nobuiltin allocsize(0)
declare nonnull i8* @_Znam(i64) #6

declare i32 @__gxx_personality_v0(...)

declare %class.QUBIT* @__quantum__rt__qubit_allocate() #2

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__16vectorIjNS_9allocatorIjEEE9push_backERKj(%"class.std::__1::vector"* %0, i32* nonnull align 4 dereferenceable(4) %1) #0 align 2 {
  %3 = alloca %"class.std::__1::vector"*, align 8
  %4 = alloca i32*, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %3, align 8
  store i32* %1, i32** %4, align 8
  %5 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  %6 = bitcast %"class.std::__1::vector"* %5 to %"class.std::__1::__vector_base"*
  %7 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %6, i32 0, i32 1
  %8 = load i32*, i32** %7, align 8
  %9 = bitcast %"class.std::__1::vector"* %5 to %"class.std::__1::__vector_base"*
  %10 = call nonnull align 8 dereferenceable(8) i32** @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEE9__end_capEv(%"class.std::__1::__vector_base"* %9) #11
  %11 = load i32*, i32** %10, align 8
  %12 = icmp ne i32* %8, %11
  br i1 %12, label %13, label %15

13:                                               ; preds = %2
  %14 = load i32*, i32** %4, align 8
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEE22__construct_one_at_endIJRKjEEEvDpOT_(%"class.std::__1::vector"* %5, i32* nonnull align 4 dereferenceable(4) %14)
  br label %17

15:                                               ; preds = %2
  %16 = load i32*, i32** %4, align 8
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEE21__push_back_slow_pathIRKjEEvOT_(%"class.std::__1::vector"* %5, i32* nonnull align 4 dereferenceable(4) %16)
  br label %17

17:                                               ; preds = %15, %13
  ret void
}

declare void @_ZN9Microsoft7Quantum19QirExecutionContext10OnAllocateEPv(%"struct.Microsoft::Quantum::QirExecutionContext"*, i8*) #2

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__16vectorIjNS_9allocatorIjEEED1Ev(%"class.std::__1::vector"* %0) unnamed_addr #1 align 2 {
  %2 = alloca %"class.std::__1::vector"*, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %2, align 8
  %3 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %2, align 8
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEED2Ev(%"class.std::__1::vector"* %3) #11
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define void @_ZN8QirArrayC1Ej(%Array* %0, i32 %1) unnamed_addr #0 align 2 {
  %3 = alloca %Array*, align 8
  %4 = alloca i32, align 4
  store %Array* %0, %Array** %3, align 8
  store i32 %1, i32* %4, align 4
  %5 = load %Array*, %Array** %3, align 8
  %6 = load i32, i32* %4, align 4
  call void @_ZN8QirArrayC2Ej(%Array* %5, i32 %6)
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define void @_ZN8QirArrayC2EjjhONSt3__16vectorIjNS0_9allocatorIjEEEE(%Array* %0, i32 %1, i32 %2, i8 zeroext %3, %"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %4) unnamed_addr #0 align 2 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %6 = alloca %Array*, align 8
  %7 = alloca i32, align 4
  %8 = alloca i32, align 4
  %9 = alloca i8, align 1
  %10 = alloca %"class.std::__1::vector"*, align 8
  %11 = alloca i8*, align 8
  %12 = alloca i32, align 4
  %13 = alloca i64, align 8
  store %Array* %0, %Array** %6, align 8
  store i32 %1, i32* %7, align 4
  store i32 %2, i32* %8, align 4
  store i8 %3, i8* %9, align 1
  store %"class.std::__1::vector"* %4, %"class.std::__1::vector"** %10, align 8
  %14 = load %Array*, %Array** %6, align 8
  %15 = getelementptr inbounds %Array, %Array* %14, i32 0, i32 0
  %16 = load i32, i32* %7, align 4
  store i32 %16, i32* %15, align 8
  %17 = getelementptr inbounds %Array, %Array* %14, i32 0, i32 1
  %18 = load i32, i32* %8, align 4
  %19 = icmp eq i32 %18, 1
  br i1 %19, label %31, label %20

20:                                               ; preds = %5
  %21 = load i32, i32* %8, align 4
  %22 = icmp eq i32 %21, 2
  br i1 %22, label %31, label %23

23:                                               ; preds = %20
  %24 = load i32, i32* %8, align 4
  %25 = icmp eq i32 %24, 4
  br i1 %25, label %31, label %26

26:                                               ; preds = %23
  %27 = load i32, i32* %8, align 4
  %28 = zext i32 %27 to i64
  %29 = urem i64 %28, 8
  %30 = icmp eq i64 %29, 0
  br i1 %30, label %31, label %34

31:                                               ; preds = %26, %23, %20, %5
  %32 = load i32, i32* %8, align 4
  %33 = zext i32 %32 to i64
  br label %42

34:                                               ; preds = %26
  %35 = load i32, i32* %8, align 4
  %36 = zext i32 %35 to i64
  %37 = add i64 %36, 8
  %38 = load i32, i32* %8, align 4
  %39 = zext i32 %38 to i64
  %40 = urem i64 %39, 8
  %41 = sub i64 %37, %40
  br label %42

42:                                               ; preds = %34, %31
  %43 = phi i64 [ %33, %31 ], [ %41, %34 ]
  %44 = trunc i64 %43 to i32
  store i32 %44, i32* %17, align 4
  %45 = getelementptr inbounds %Array, %Array* %14, i32 0, i32 2
  %46 = load i8, i8* %9, align 1
  store i8 %46, i8* %45, align 8
  %47 = getelementptr inbounds %Array, %Array* %14, i32 0, i32 4
  %48 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %10, align 8
  %49 = call nonnull align 8 dereferenceable(24) %"class.std::__1::vector"* @_ZNSt3__14moveIRNS_6vectorIjNS_9allocatorIjEEEEEEONS_16remove_referenceIT_E4typeEOS7_(%"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %48) #11
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEEC1EOS3_(%"class.std::__1::vector"* %47, %"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %49) #11
  %50 = getelementptr inbounds %Array, %Array* %14, i32 0, i32 5
  store i8* null, i8** %50, align 8
  %51 = getelementptr inbounds %Array, %Array* %14, i32 0, i32 6
  store i8 0, i8* %51, align 8
  %52 = getelementptr inbounds %Array, %Array* %14, i32 0, i32 8
  store i32 1, i32* %52, align 4
  %53 = getelementptr inbounds %Array, %Array* %14, i32 0, i32 9
  store i32 0, i32* %53, align 8
  %54 = load i32, i32* %8, align 4
  %55 = icmp ne i32 %54, 0
  %56 = xor i1 %55, true
  br i1 %56, label %57, label %64

57:                                               ; preds = %42
  invoke void @__assert_rtn(i8* getelementptr inbounds ([9 x i8], [9 x i8]* @__func__._ZN8QirArrayC2EjjhONSt3__16vectorIjNS0_9allocatorIjEEEE, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 102, i8* getelementptr inbounds ([19 x i8], [19 x i8]* @.str.5, i64 0, i64 0)) #14
          to label %58 unwind label %59

58:                                               ; preds = %57
  unreachable

59:                                               ; preds = %145, %136, %123, %108, %101, %80, %78, %74, %70, %57
  %60 = landingpad { i8*, i32 }
          cleanup
  %61 = extractvalue { i8*, i32 } %60, 0
  store i8* %61, i8** %11, align 8
  %62 = extractvalue { i8*, i32 } %60, 1
  store i32 %62, i32* %12, align 4
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEED1Ev(%"class.std::__1::vector"* %47) #11
  br label %156

63:                                               ; No predecessors!
  br label %65

64:                                               ; preds = %42
  br label %65

65:                                               ; preds = %64, %63
  %66 = load i8, i8* %9, align 1
  %67 = zext i8 %66 to i32
  %68 = icmp sgt i32 %67, 0
  %69 = xor i1 %68, true
  br i1 %69, label %70, label %73

70:                                               ; preds = %65
  invoke void @__assert_rtn(i8* getelementptr inbounds ([9 x i8], [9 x i8]* @__func__._ZN8QirArrayC2EjjhONSt3__16vectorIjNS0_9allocatorIjEEEE, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 103, i8* getelementptr inbounds ([13 x i8], [13 x i8]* @.str.6, i64 0, i64 0)) #14
          to label %71 unwind label %59

71:                                               ; preds = %70
  unreachable

72:                                               ; No predecessors!
  br label %74

73:                                               ; preds = %65
  br label %74

74:                                               ; preds = %73, %72
  %75 = invoke nonnull align 8 dereferenceable(8) %"class.std::__1::unique_ptr"* @_ZN9Microsoft7Quantum13GlobalContextEv()
          to label %76 unwind label %59

76:                                               ; preds = %74
  %77 = call zeroext i1 @_ZNSt3__1neIN9Microsoft7Quantum19QirExecutionContextENS_14default_deleteIS3_EEEEbRKNS_10unique_ptrIT_T0_EEDn(%"class.std::__1::unique_ptr"* nonnull align 8 dereferenceable(8) %75, i8* null) #11
  br i1 %77, label %78, label %84

78:                                               ; preds = %76
  %79 = invoke nonnull align 8 dereferenceable(8) %"class.std::__1::unique_ptr"* @_ZN9Microsoft7Quantum13GlobalContextEv()
          to label %80 unwind label %59

80:                                               ; preds = %78
  %81 = call %"struct.Microsoft::Quantum::QirExecutionContext"* @_ZNKSt3__110unique_ptrIN9Microsoft7Quantum19QirExecutionContextENS_14default_deleteIS3_EEEptEv(%"class.std::__1::unique_ptr"* %79) #11
  %82 = bitcast %Array* %14 to i8*
  invoke void @_ZN9Microsoft7Quantum19QirExecutionContext10OnAllocateEPv(%"struct.Microsoft::Quantum::QirExecutionContext"* %81, i8* %82)
          to label %83 unwind label %59

83:                                               ; preds = %80
  br label %84

84:                                               ; preds = %83, %76
  %85 = load i8, i8* %9, align 1
  %86 = zext i8 %85 to i32
  %87 = icmp eq i32 %86, 1
  br i1 %87, label %88, label %112

88:                                               ; preds = %84
  %89 = getelementptr inbounds %Array, %Array* %14, i32 0, i32 4
  %90 = call zeroext i1 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE5emptyEv(%"class.std::__1::vector"* %89) #11
  br i1 %90, label %98, label %91

91:                                               ; preds = %88
  %92 = getelementptr inbounds %Array, %Array* %14, i32 0, i32 4
  %93 = call nonnull align 4 dereferenceable(4) i32* @_ZNSt3__16vectorIjNS_9allocatorIjEEEixEm(%"class.std::__1::vector"* %92, i64 0) #11
  %94 = load i32, i32* %93, align 4
  %95 = getelementptr inbounds %Array, %Array* %14, i32 0, i32 0
  %96 = load i32, i32* %95, align 8
  %97 = icmp eq i32 %94, %96
  br label %98

98:                                               ; preds = %91, %88
  %99 = phi i1 [ true, %88 ], [ %97, %91 ]
  %100 = xor i1 %99, true
  br i1 %100, label %101, label %104

101:                                              ; preds = %98
  invoke void @__assert_rtn(i8* getelementptr inbounds ([9 x i8], [9 x i8]* @__func__._ZN8QirArrayC2EjjhONSt3__16vectorIjNS0_9allocatorIjEEEE, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 112, i8* getelementptr inbounds ([71 x i8], [71 x i8]* @.str.7, i64 0, i64 0)) #14
          to label %102 unwind label %59

102:                                              ; preds = %101
  unreachable

103:                                              ; No predecessors!
  br label %105

104:                                              ; preds = %98
  br label %105

105:                                              ; preds = %104, %103
  %106 = getelementptr inbounds %Array, %Array* %14, i32 0, i32 4
  %107 = call zeroext i1 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE5emptyEv(%"class.std::__1::vector"* %106) #11
  br i1 %107, label %108, label %111

108:                                              ; preds = %105
  %109 = getelementptr inbounds %Array, %Array* %14, i32 0, i32 4
  invoke void @_ZNSt3__16vectorIjNS_9allocatorIjEEE9push_backERKj(%"class.std::__1::vector"* %109, i32* nonnull align 4 dereferenceable(4) %7)
          to label %110 unwind label %59

110:                                              ; preds = %108
  br label %111

111:                                              ; preds = %110, %105
  br label %112

112:                                              ; preds = %111, %84
  %113 = getelementptr inbounds %Array, %Array* %14, i32 0, i32 0
  %114 = load i32, i32* %113, align 8
  %115 = zext i32 %114 to i64
  %116 = getelementptr inbounds %Array, %Array* %14, i32 0, i32 1
  %117 = load i32, i32* %116, align 4
  %118 = zext i32 %117 to i64
  %119 = mul i64 %115, %118
  %120 = call i64 @_ZNSt3__114numeric_limitsImE3maxEv() #11
  %121 = icmp ult i64 %119, %120
  %122 = xor i1 %121, true
  br i1 %122, label %123, label %126

123:                                              ; preds = %112
  invoke void @__assert_rtn(i8* getelementptr inbounds ([9 x i8], [9 x i8]* @__func__._ZN8QirArrayC2EjjhONSt3__16vectorIjNS0_9allocatorIjEEEE, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 119, i8* getelementptr inbounds ([79 x i8], [79 x i8]* @.str.8, i64 0, i64 0)) #14
          to label %124 unwind label %59

124:                                              ; preds = %123
  unreachable

125:                                              ; No predecessors!
  br label %127

126:                                              ; preds = %112
  br label %127

127:                                              ; preds = %126, %125
  %128 = getelementptr inbounds %Array, %Array* %14, i32 0, i32 0
  %129 = load i32, i32* %128, align 8
  %130 = getelementptr inbounds %Array, %Array* %14, i32 0, i32 1
  %131 = load i32, i32* %130, align 4
  %132 = mul i32 %129, %131
  %133 = zext i32 %132 to i64
  store i64 %133, i64* %13, align 8
  %134 = load i64, i64* %13, align 8
  %135 = icmp ugt i64 %134, 0
  br i1 %135, label %136, label %153

136:                                              ; preds = %127
  %137 = load i64, i64* %13, align 8
  %138 = invoke noalias nonnull i8* @_Znam(i64 %137) #16
          to label %139 unwind label %59

139:                                              ; preds = %136
  %140 = getelementptr inbounds %Array, %Array* %14, i32 0, i32 5
  store i8* %138, i8** %140, align 8
  %141 = load i64, i64* %13, align 8
  %142 = call i64 @_ZNSt3__114numeric_limitsImE3maxEv() #11
  %143 = icmp ule i64 %141, %142
  %144 = xor i1 %143, true
  br i1 %144, label %145, label %148

145:                                              ; preds = %139
  invoke void @__assert_rtn(i8* getelementptr inbounds ([9 x i8], [9 x i8]* @__func__._ZN8QirArrayC2EjjhONSt3__16vectorIjNS0_9allocatorIjEEEE, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 125, i8* getelementptr inbounds ([49 x i8], [49 x i8]* @.str.9, i64 0, i64 0)) #14
          to label %146 unwind label %59

146:                                              ; preds = %145
  unreachable

147:                                              ; No predecessors!
  br label %149

148:                                              ; preds = %139
  br label %149

149:                                              ; preds = %148, %147
  %150 = getelementptr inbounds %Array, %Array* %14, i32 0, i32 5
  %151 = load i8*, i8** %150, align 8
  %152 = load i64, i64* %13, align 8
  call void @llvm.memset.p0i8.i64(i8* align 1 %151, i8 0, i64 %152, i1 false)
  br label %155

153:                                              ; preds = %127
  %154 = getelementptr inbounds %Array, %Array* %14, i32 0, i32 5
  store i8* null, i8** %154, align 8
  br label %155

155:                                              ; preds = %153, %149
  ret void

156:                                              ; preds = %59
  %157 = load i8*, i8** %11, align 8
  %158 = load i32, i32* %12, align 4
  %159 = insertvalue { i8*, i32 } undef, i8* %157, 0
  %160 = insertvalue { i8*, i32 } %159, i32 %158, 1
  resume { i8*, i32 } %160
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(24) %"class.std::__1::vector"* @_ZNSt3__14moveIRNS_6vectorIjNS_9allocatorIjEEEEEEONS_16remove_referenceIT_E4typeEOS7_(%"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %0) #1 {
  %2 = alloca %"class.std::__1::vector"*, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %2, align 8
  %3 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %2, align 8
  ret %"class.std::__1::vector"* %3
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__16vectorIjNS_9allocatorIjEEEC1EOS3_(%"class.std::__1::vector"* %0, %"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %1) unnamed_addr #1 align 2 {
  %3 = alloca %"class.std::__1::vector"*, align 8
  %4 = alloca %"class.std::__1::vector"*, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %3, align 8
  store %"class.std::__1::vector"* %1, %"class.std::__1::vector"** %4, align 8
  %5 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  %6 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %4, align 8
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEEC2EOS3_(%"class.std::__1::vector"* %5, %"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %6) #11
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden zeroext i1 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE5emptyEv(%"class.std::__1::vector"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::vector"*, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %2, align 8
  %3 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %2, align 8
  %4 = bitcast %"class.std::__1::vector"* %3 to %"class.std::__1::__vector_base"*
  %5 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %4, i32 0, i32 0
  %6 = load i32*, i32** %5, align 8
  %7 = bitcast %"class.std::__1::vector"* %3 to %"class.std::__1::__vector_base"*
  %8 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %7, i32 0, i32 1
  %9 = load i32*, i32** %8, align 8
  %10 = icmp eq i32* %6, %9
  ret i1 %10
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 4 dereferenceable(4) i32* @_ZNSt3__16vectorIjNS_9allocatorIjEEEixEm(%"class.std::__1::vector"* %0, i64 %1) #1 align 2 {
  %3 = alloca %"class.std::__1::vector"*, align 8
  %4 = alloca i64, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %3, align 8
  store i64 %1, i64* %4, align 8
  %5 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  %6 = bitcast %"class.std::__1::vector"* %5 to %"class.std::__1::__vector_base"*
  %7 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %6, i32 0, i32 0
  %8 = load i32*, i32** %7, align 8
  %9 = load i64, i64* %4, align 8
  %10 = getelementptr inbounds i32, i32* %8, i64 %9
  ret i32* %10
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i64 @_ZNSt3__114numeric_limitsImE3maxEv() #1 align 2 {
  %1 = call i64 @_ZNSt3__123__libcpp_numeric_limitsImLb1EE3maxEv() #11
  ret i64 %1
}

; Function Attrs: argmemonly nounwind willreturn writeonly
declare void @llvm.memset.p0i8.i64(i8* nocapture writeonly, i8, i64, i1 immarg) #7

; Function Attrs: noinline optnone ssp uwtable
define void @_ZN8QirArrayC1EjjhONSt3__16vectorIjNS0_9allocatorIjEEEE(%Array* %0, i32 %1, i32 %2, i8 zeroext %3, %"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %4) unnamed_addr #0 align 2 {
  %6 = alloca %Array*, align 8
  %7 = alloca i32, align 4
  %8 = alloca i32, align 4
  %9 = alloca i8, align 1
  %10 = alloca %"class.std::__1::vector"*, align 8
  store %Array* %0, %Array** %6, align 8
  store i32 %1, i32* %7, align 4
  store i32 %2, i32* %8, align 4
  store i8 %3, i8* %9, align 1
  store %"class.std::__1::vector"* %4, %"class.std::__1::vector"** %10, align 8
  %11 = load %Array*, %Array** %6, align 8
  %12 = load i32, i32* %7, align 4
  %13 = load i32, i32* %8, align 4
  %14 = load i8, i8* %9, align 1
  %15 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %10, align 8
  call void @_ZN8QirArrayC2EjjhONSt3__16vectorIjNS0_9allocatorIjEEEE(%Array* %11, i32 %12, i32 %13, i8 zeroext %14, %"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %15)
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define void @_ZN8QirArrayC2ERKS_(%Array* %0, %Array* nonnull align 8 dereferenceable(60) %1) unnamed_addr #0 align 2 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %3 = alloca %Array*, align 8
  %4 = alloca %Array*, align 8
  %5 = alloca i8*, align 8
  %6 = alloca i32, align 4
  %7 = alloca i64, align 8
  store %Array* %0, %Array** %3, align 8
  store %Array* %1, %Array** %4, align 8
  %8 = load %Array*, %Array** %3, align 8
  %9 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 0
  %10 = load %Array*, %Array** %4, align 8
  %11 = getelementptr inbounds %Array, %Array* %10, i32 0, i32 0
  %12 = load i32, i32* %11, align 8
  store i32 %12, i32* %9, align 8
  %13 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 1
  %14 = load %Array*, %Array** %4, align 8
  %15 = getelementptr inbounds %Array, %Array* %14, i32 0, i32 1
  %16 = load i32, i32* %15, align 4
  store i32 %16, i32* %13, align 4
  %17 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 2
  %18 = load %Array*, %Array** %4, align 8
  %19 = getelementptr inbounds %Array, %Array* %18, i32 0, i32 2
  %20 = load i8, i8* %19, align 8
  store i8 %20, i8* %17, align 8
  %21 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 4
  %22 = load %Array*, %Array** %4, align 8
  %23 = getelementptr inbounds %Array, %Array* %22, i32 0, i32 4
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEEC1ERKS3_(%"class.std::__1::vector"* %21, %"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %23)
  %24 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 5
  store i8* null, i8** %24, align 8
  %25 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 6
  store i8 0, i8* %25, align 8
  %26 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 8
  store i32 1, i32* %26, align 4
  %27 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 9
  store i32 0, i32* %27, align 8
  %28 = invoke nonnull align 8 dereferenceable(8) %"class.std::__1::unique_ptr"* @_ZN9Microsoft7Quantum13GlobalContextEv()
          to label %29 unwind label %37

29:                                               ; preds = %2
  %30 = call zeroext i1 @_ZNSt3__1neIN9Microsoft7Quantum19QirExecutionContextENS_14default_deleteIS3_EEEEbRKNS_10unique_ptrIT_T0_EEDn(%"class.std::__1::unique_ptr"* nonnull align 8 dereferenceable(8) %28, i8* null) #11
  br i1 %30, label %31, label %41

31:                                               ; preds = %29
  %32 = invoke nonnull align 8 dereferenceable(8) %"class.std::__1::unique_ptr"* @_ZN9Microsoft7Quantum13GlobalContextEv()
          to label %33 unwind label %37

33:                                               ; preds = %31
  %34 = call %"struct.Microsoft::Quantum::QirExecutionContext"* @_ZNKSt3__110unique_ptrIN9Microsoft7Quantum19QirExecutionContextENS_14default_deleteIS3_EEEptEv(%"class.std::__1::unique_ptr"* %32) #11
  %35 = bitcast %Array* %8 to i8*
  invoke void @_ZN9Microsoft7Quantum19QirExecutionContext10OnAllocateEPv(%"struct.Microsoft::Quantum::QirExecutionContext"* %34, i8* %35)
          to label %36 unwind label %37

36:                                               ; preds = %33
  br label %41

37:                                               ; preds = %75, %66, %52, %33, %31, %2
  %38 = landingpad { i8*, i32 }
          cleanup
  %39 = extractvalue { i8*, i32 } %38, 0
  store i8* %39, i8** %5, align 8
  %40 = extractvalue { i8*, i32 } %38, 1
  store i32 %40, i32* %6, align 4
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEED1Ev(%"class.std::__1::vector"* %21) #11
  br label %89

41:                                               ; preds = %36, %29
  %42 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 0
  %43 = load i32, i32* %42, align 8
  %44 = zext i32 %43 to i64
  %45 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 1
  %46 = load i32, i32* %45, align 4
  %47 = zext i32 %46 to i64
  %48 = mul i64 %44, %47
  %49 = call i64 @_ZNSt3__114numeric_limitsImE3maxEv() #11
  %50 = icmp ult i64 %48, %49
  %51 = xor i1 %50, true
  br i1 %51, label %52, label %55

52:                                               ; preds = %41
  invoke void @__assert_rtn(i8* getelementptr inbounds ([9 x i8], [9 x i8]* @__func__._ZN8QirArrayC2EjjhONSt3__16vectorIjNS0_9allocatorIjEEEE, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 147, i8* getelementptr inbounds ([87 x i8], [87 x i8]* @.str.10, i64 0, i64 0)) #14
          to label %53 unwind label %37

53:                                               ; preds = %52
  unreachable

54:                                               ; No predecessors!
  br label %56

55:                                               ; preds = %41
  br label %56

56:                                               ; preds = %55, %54
  %57 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 0
  %58 = load i32, i32* %57, align 8
  %59 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 1
  %60 = load i32, i32* %59, align 4
  %61 = mul i32 %58, %60
  %62 = zext i32 %61 to i64
  store i64 %62, i64* %7, align 8
  %63 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 0
  %64 = load i32, i32* %63, align 8
  %65 = icmp ugt i32 %64, 0
  br i1 %65, label %66, label %86

66:                                               ; preds = %56
  %67 = load i64, i64* %7, align 8
  %68 = invoke noalias nonnull i8* @_Znam(i64 %67) #16
          to label %69 unwind label %37

69:                                               ; preds = %66
  %70 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 5
  store i8* %68, i8** %70, align 8
  %71 = load i64, i64* %7, align 8
  %72 = call i64 @_ZNSt3__114numeric_limitsImE3maxEv() #11
  %73 = icmp ule i64 %71, %72
  %74 = xor i1 %73, true
  br i1 %74, label %75, label %78

75:                                               ; preds = %69
  invoke void @__assert_rtn(i8* getelementptr inbounds ([9 x i8], [9 x i8]* @__func__._ZN8QirArrayC2EjjhONSt3__16vectorIjNS0_9allocatorIjEEEE, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 153, i8* getelementptr inbounds ([43 x i8], [43 x i8]* @.str.11, i64 0, i64 0)) #14
          to label %76 unwind label %37

76:                                               ; preds = %75
  unreachable

77:                                               ; No predecessors!
  br label %79

78:                                               ; preds = %69
  br label %79

79:                                               ; preds = %78, %77
  %80 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 5
  %81 = load i8*, i8** %80, align 8
  %82 = load %Array*, %Array** %4, align 8
  %83 = getelementptr inbounds %Array, %Array* %82, i32 0, i32 5
  %84 = load i8*, i8** %83, align 8
  %85 = load i64, i64* %7, align 8
  call void @llvm.memcpy.p0i8.p0i8.i64(i8* align 1 %81, i8* align 1 %84, i64 %85, i1 false)
  br label %88

86:                                               ; preds = %56
  %87 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 5
  store i8* null, i8** %87, align 8
  br label %88

88:                                               ; preds = %86, %79
  ret void

89:                                               ; preds = %37
  %90 = load i8*, i8** %5, align 8
  %91 = load i32, i32* %6, align 4
  %92 = insertvalue { i8*, i32 } undef, i8* %90, 0
  %93 = insertvalue { i8*, i32 } %92, i32 %91, 1
  resume { i8*, i32 } %93
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__16vectorIjNS_9allocatorIjEEEC1ERKS3_(%"class.std::__1::vector"* %0, %"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %1) unnamed_addr #0 align 2 {
  %3 = alloca %"class.std::__1::vector"*, align 8
  %4 = alloca %"class.std::__1::vector"*, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %3, align 8
  store %"class.std::__1::vector"* %1, %"class.std::__1::vector"** %4, align 8
  %5 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  %6 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %4, align 8
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEEC2ERKS3_(%"class.std::__1::vector"* %5, %"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %6)
  ret void
}

; Function Attrs: argmemonly nounwind willreturn
declare void @llvm.memcpy.p0i8.p0i8.i64(i8* noalias nocapture writeonly, i8* noalias nocapture readonly, i64, i1 immarg) #8

; Function Attrs: noinline optnone ssp uwtable
define void @_ZN8QirArrayC1ERKS_(%Array* %0, %Array* nonnull align 8 dereferenceable(60) %1) unnamed_addr #0 align 2 {
  %3 = alloca %Array*, align 8
  %4 = alloca %Array*, align 8
  store %Array* %0, %Array** %3, align 8
  store %Array* %1, %Array** %4, align 8
  %5 = load %Array*, %Array** %3, align 8
  %6 = load %Array*, %Array** %4, align 8
  call void @_ZN8QirArrayC2ERKS_(%Array* %5, %Array* nonnull align 8 dereferenceable(60) %6)
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define void @_ZN8QirArrayD2Ev(%Array* %0) unnamed_addr #1 align 2 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %2 = alloca %Array*, align 8
  %3 = alloca i8*, align 8
  %4 = alloca i32, align 4
  store %Array* %0, %Array** %2, align 8
  %5 = load %Array*, %Array** %2, align 8
  %6 = getelementptr inbounds %Array, %Array* %5, i32 0, i32 5
  %7 = load i8*, i8** %6, align 8
  %8 = icmp eq i8* %7, null
  %9 = xor i1 %8, true
  br i1 %9, label %10, label %18

10:                                               ; preds = %1
  invoke void @__assert_rtn(i8* getelementptr inbounds ([10 x i8], [10 x i8]* @__func__._ZN8QirArrayD2Ev, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 164, i8* getelementptr inbounds ([24 x i8], [24 x i8]* @.str.12, i64 0, i64 0)) #14
          to label %11 unwind label %12

11:                                               ; preds = %10
  unreachable

12:                                               ; preds = %10
  %13 = landingpad { i8*, i32 }
          catch i8* null
  %14 = extractvalue { i8*, i32 } %13, 0
  store i8* %14, i8** %3, align 8
  %15 = extractvalue { i8*, i32 } %13, 1
  store i32 %15, i32* %4, align 4
  %16 = getelementptr inbounds %Array, %Array* %5, i32 0, i32 4
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEED1Ev(%"class.std::__1::vector"* %16) #11
  br label %21

17:                                               ; No predecessors!
  br label %19

18:                                               ; preds = %1
  br label %19

19:                                               ; preds = %18, %17
  %20 = getelementptr inbounds %Array, %Array* %5, i32 0, i32 4
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEED1Ev(%"class.std::__1::vector"* %20) #11
  ret void

21:                                               ; preds = %12
  %22 = load i8*, i8** %3, align 8
  call void @__clang_call_terminate(i8* %22) #17
  unreachable
}

; Function Attrs: noinline noreturn nounwind
define linkonce_odr hidden void @__clang_call_terminate(i8* %0) #9 {
  %2 = call i8* @__cxa_begin_catch(i8* %0) #11
  call void @_ZSt9terminatev() #17
  unreachable
}

declare i8* @__cxa_begin_catch(i8*)

declare void @_ZSt9terminatev()

; Function Attrs: noinline nounwind optnone ssp uwtable
define void @_ZN8QirArrayD1Ev(%Array* %0) unnamed_addr #1 align 2 {
  %2 = alloca %Array*, align 8
  store %Array* %0, %Array** %2, align 8
  %3 = load %Array*, %Array** %2, align 8
  call void @_ZN8QirArrayD2Ev(%Array* %3) #11
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define i8* @_ZNK8QirArray14GetItemPointerEj(%Array* %0, i32 %1) #0 align 2 {
  %3 = alloca %Array*, align 8
  %4 = alloca i32, align 4
  store %Array* %0, %Array** %3, align 8
  store i32 %1, i32* %4, align 4
  %5 = load %Array*, %Array** %3, align 8
  %6 = load i32, i32* %4, align 4
  %7 = getelementptr inbounds %Array, %Array* %5, i32 0, i32 0
  %8 = load i32, i32* %7, align 8
  %9 = icmp ult i32 %6, %8
  %10 = xor i1 %9, true
  br i1 %10, label %11, label %13

11:                                               ; preds = %2
  call void @__assert_rtn(i8* getelementptr inbounds ([15 x i8], [15 x i8]* @__func__._ZNK8QirArray14GetItemPointerEj, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 169, i8* getelementptr inbounds ([20 x i8], [20 x i8]* @.str.13, i64 0, i64 0)) #14
  unreachable

12:                                               ; No predecessors!
  br label %14

13:                                               ; preds = %2
  br label %14

14:                                               ; preds = %13, %12
  %15 = getelementptr inbounds %Array, %Array* %5, i32 0, i32 5
  %16 = load i8*, i8** %15, align 8
  %17 = load i32, i32* %4, align 4
  %18 = getelementptr inbounds %Array, %Array* %5, i32 0, i32 1
  %19 = load i32, i32* %18, align 4
  %20 = mul i32 %17, %19
  %21 = zext i32 %20 to i64
  %22 = getelementptr inbounds i8, i8* %16, i64 %21
  ret i8* %22
}

; Function Attrs: noinline optnone ssp uwtable
define void @_ZN8QirArray6AppendEPKS_(%Array* %0, %Array* %1) #0 align 2 {
  %3 = alloca %Array*, align 8
  %4 = alloca %Array*, align 8
  %5 = alloca i64, align 8
  %6 = alloca i64, align 8
  %7 = alloca i8*, align 8
  store %Array* %0, %Array** %3, align 8
  store %Array* %1, %Array** %4, align 8
  %8 = load %Array*, %Array** %3, align 8
  %9 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 6
  %10 = load i8, i8* %9, align 8
  %11 = trunc i8 %10 to i1
  %12 = xor i1 %11, true
  %13 = xor i1 %12, true
  br i1 %13, label %14, label %16

14:                                               ; preds = %2
  call void @__assert_rtn(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @__func__._ZN8QirArray6AppendEPKS_, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 175, i8* getelementptr inbounds ([18 x i8], [18 x i8]* @.str.14, i64 0, i64 0)) #14
  unreachable

15:                                               ; No predecessors!
  br label %17

16:                                               ; preds = %2
  br label %17

17:                                               ; preds = %16, %15
  %18 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 1
  %19 = load i32, i32* %18, align 4
  %20 = load %Array*, %Array** %4, align 8
  %21 = getelementptr inbounds %Array, %Array* %20, i32 0, i32 1
  %22 = load i32, i32* %21, align 4
  %23 = icmp eq i32 %19, %22
  %24 = xor i1 %23, true
  br i1 %24, label %25, label %27

25:                                               ; preds = %17
  call void @__assert_rtn(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @__func__._ZN8QirArray6AppendEPKS_, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 176, i8* getelementptr inbounds ([48 x i8], [48 x i8]* @.str.15, i64 0, i64 0)) #14
  unreachable

26:                                               ; No predecessors!
  br label %28

27:                                               ; preds = %17
  br label %28

28:                                               ; preds = %27, %26
  %29 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 2
  %30 = load i8, i8* %29, align 8
  %31 = zext i8 %30 to i32
  %32 = icmp eq i32 %31, 1
  br i1 %32, label %33, label %39

33:                                               ; preds = %28
  %34 = load %Array*, %Array** %4, align 8
  %35 = getelementptr inbounds %Array, %Array* %34, i32 0, i32 2
  %36 = load i8, i8* %35, align 8
  %37 = zext i8 %36 to i32
  %38 = icmp eq i32 %37, 1
  br label %39

39:                                               ; preds = %33, %28
  %40 = phi i1 [ false, %28 ], [ %38, %33 ]
  %41 = xor i1 %40, true
  br i1 %41, label %42, label %44

42:                                               ; preds = %39
  call void @__assert_rtn(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @__func__._ZN8QirArray6AppendEPKS_, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 177, i8* getelementptr inbounds ([48 x i8], [48 x i8]* @.str.16, i64 0, i64 0)) #14
  unreachable

43:                                               ; No predecessors!
  br label %45

44:                                               ; preds = %39
  br label %45

45:                                               ; preds = %44, %43
  %46 = load %Array*, %Array** %4, align 8
  %47 = getelementptr inbounds %Array, %Array* %46, i32 0, i32 0
  %48 = load i32, i32* %47, align 8
  %49 = zext i32 %48 to i64
  %50 = load %Array*, %Array** %4, align 8
  %51 = getelementptr inbounds %Array, %Array* %50, i32 0, i32 1
  %52 = load i32, i32* %51, align 4
  %53 = zext i32 %52 to i64
  %54 = mul i64 %49, %53
  %55 = call i64 @_ZNSt3__114numeric_limitsImE3maxEv() #11
  %56 = icmp ult i64 %54, %55
  %57 = xor i1 %56, true
  br i1 %57, label %58, label %60

58:                                               ; preds = %45
  call void @__assert_rtn(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @__func__._ZN8QirArray6AppendEPKS_, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 179, i8* getelementptr inbounds ([89 x i8], [89 x i8]* @.str.17, i64 0, i64 0)) #14
  unreachable

59:                                               ; No predecessors!
  br label %61

60:                                               ; preds = %45
  br label %61

61:                                               ; preds = %60, %59
  %62 = load %Array*, %Array** %4, align 8
  %63 = getelementptr inbounds %Array, %Array* %62, i32 0, i32 0
  %64 = load i32, i32* %63, align 8
  %65 = load %Array*, %Array** %4, align 8
  %66 = getelementptr inbounds %Array, %Array* %65, i32 0, i32 1
  %67 = load i32, i32* %66, align 4
  %68 = mul i32 %64, %67
  %69 = zext i32 %68 to i64
  store i64 %69, i64* %5, align 8
  %70 = load i64, i64* %5, align 8
  %71 = icmp eq i64 %70, 0
  br i1 %71, label %72, label %73

72:                                               ; preds = %61
  br label %130

73:                                               ; preds = %61
  %74 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 0
  %75 = load i32, i32* %74, align 8
  %76 = zext i32 %75 to i64
  %77 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 1
  %78 = load i32, i32* %77, align 4
  %79 = zext i32 %78 to i64
  %80 = mul i64 %76, %79
  %81 = call i64 @_ZNSt3__114numeric_limitsImE3maxEv() #11
  %82 = icmp ult i64 %80, %81
  %83 = xor i1 %82, true
  br i1 %83, label %84, label %86

84:                                               ; preds = %73
  call void @__assert_rtn(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @__func__._ZN8QirArray6AppendEPKS_, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 188, i8* getelementptr inbounds ([87 x i8], [87 x i8]* @.str.10, i64 0, i64 0)) #14
  unreachable

85:                                               ; No predecessors!
  br label %87

86:                                               ; preds = %73
  br label %87

87:                                               ; preds = %86, %85
  %88 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 0
  %89 = load i32, i32* %88, align 8
  %90 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 1
  %91 = load i32, i32* %90, align 4
  %92 = mul i32 %89, %91
  %93 = zext i32 %92 to i64
  store i64 %93, i64* %6, align 8
  %94 = load i64, i64* %6, align 8
  %95 = load i64, i64* %5, align 8
  %96 = add i64 %94, %95
  %97 = call noalias nonnull i8* @_Znam(i64 %96) #16
  store i8* %97, i8** %7, align 8
  %98 = load i64, i64* %6, align 8
  %99 = icmp ne i64 %98, 0
  br i1 %99, label %100, label %105

100:                                              ; preds = %87
  %101 = load i8*, i8** %7, align 8
  %102 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 5
  %103 = load i8*, i8** %102, align 8
  %104 = load i64, i64* %6, align 8
  call void @llvm.memcpy.p0i8.p0i8.i64(i8* align 1 %101, i8* align 1 %103, i64 %104, i1 false)
  br label %105

105:                                              ; preds = %100, %87
  %106 = load i8*, i8** %7, align 8
  %107 = load i64, i64* %6, align 8
  %108 = getelementptr inbounds i8, i8* %106, i64 %107
  %109 = load %Array*, %Array** %4, align 8
  %110 = getelementptr inbounds %Array, %Array* %109, i32 0, i32 5
  %111 = load i8*, i8** %110, align 8
  %112 = load i64, i64* %5, align 8
  call void @llvm.memcpy.p0i8.p0i8.i64(i8* align 1 %108, i8* align 1 %111, i64 %112, i1 false)
  %113 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 5
  %114 = load i8*, i8** %113, align 8
  %115 = icmp eq i8* %114, null
  br i1 %115, label %117, label %116

116:                                              ; preds = %105
  call void @_ZdaPv(i8* %114) #15
  br label %117

117:                                              ; preds = %116, %105
  %118 = load i8*, i8** %7, align 8
  %119 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 5
  store i8* %118, i8** %119, align 8
  %120 = load %Array*, %Array** %4, align 8
  %121 = getelementptr inbounds %Array, %Array* %120, i32 0, i32 0
  %122 = load i32, i32* %121, align 8
  %123 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 0
  %124 = load i32, i32* %123, align 8
  %125 = add i32 %124, %122
  store i32 %125, i32* %123, align 8
  %126 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 0
  %127 = load i32, i32* %126, align 8
  %128 = getelementptr inbounds %Array, %Array* %8, i32 0, i32 4
  %129 = call nonnull align 4 dereferenceable(4) i32* @_ZNSt3__16vectorIjNS_9allocatorIjEEEixEm(%"class.std::__1::vector"* %128, i64 0) #11
  store i32 %127, i32* %129, align 4
  br label %130

130:                                              ; preds = %117, %72
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define %Array* @__quantum__rt__qubit_allocate_array(i64 %0) #0 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %2 = alloca i64, align 8
  %3 = alloca i8*, align 8
  %4 = alloca i32, align 4
  store i64 %0, i64* %2, align 8
  %5 = call noalias nonnull i8* @_Znwm(i64 64) #16
  %6 = bitcast i8* %5 to %Array*
  %7 = load i64, i64* %2, align 8
  %8 = trunc i64 %7 to i32
  invoke void @_ZN8QirArrayC1Ej(%Array* %6, i32 %8)
          to label %9 unwind label %10

9:                                                ; preds = %1
  ret %Array* %6

10:                                               ; preds = %1
  %11 = landingpad { i8*, i32 }
          cleanup
  %12 = extractvalue { i8*, i32 } %11, 0
  store i8* %12, i8** %3, align 8
  %13 = extractvalue { i8*, i32 } %11, 1
  store i32 %13, i32* %4, align 4
  call void @_ZdlPv(i8* %5) #15
  br label %14

14:                                               ; preds = %10
  %15 = load i8*, i8** %3, align 8
  %16 = load i32, i32* %4, align 4
  %17 = insertvalue { i8*, i32 } undef, i8* %15, 0
  %18 = insertvalue { i8*, i32 } %17, i32 %16, 1
  resume { i8*, i32 } %18
}

; Function Attrs: nobuiltin allocsize(0)
declare nonnull i8* @_Znwm(i64) #6

; Function Attrs: nobuiltin nounwind
declare void @_ZdlPv(i8*) #4

; Function Attrs: noinline optnone ssp uwtable
define %Array* @__quantum__rt__qubit_borrow_array(i64 %0) #0 {
  %2 = alloca i64, align 8
  store i64 %0, i64* %2, align 8
  %3 = load i64, i64* %2, align 8
  %4 = call %Array* @__quantum__rt__qubit_allocate_array(i64 %3)
  ret %Array* %4
}

; Function Attrs: noinline optnone ssp uwtable
define void @__quantum__rt__qubit_release_array(%Array* %0) #0 {
  %2 = alloca %Array*, align 8
  %3 = alloca i64*, align 8
  %4 = alloca i32, align 4
  store %Array* %0, %Array** %2, align 8
  %5 = load %Array*, %Array** %2, align 8
  %6 = icmp eq %Array* %5, null
  br i1 %6, label %7, label %8

7:                                                ; preds = %1
  br label %46

8:                                                ; preds = %1
  %9 = load %Array*, %Array** %2, align 8
  %10 = getelementptr inbounds %Array, %Array* %9, i32 0, i32 6
  %11 = load i8, i8* %10, align 8
  %12 = trunc i8 %11 to i1
  %13 = xor i1 %12, true
  br i1 %13, label %14, label %16

14:                                               ; preds = %8
  call void @__assert_rtn(i8* getelementptr inbounds ([35 x i8], [35 x i8]* @__func__.__quantum__rt__qubit_release_array, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 263, i8* getelementptr inbounds ([15 x i8], [15 x i8]* @.str.18, i64 0, i64 0)) #14
  unreachable

15:                                               ; No predecessors!
  br label %17

16:                                               ; preds = %8
  br label %17

17:                                               ; preds = %16, %15
  %18 = load %Array*, %Array** %2, align 8
  %19 = getelementptr inbounds %Array, %Array* %18, i32 0, i32 6
  %20 = load i8, i8* %19, align 8
  %21 = trunc i8 %20 to i1
  br i1 %21, label %22, label %44

22:                                               ; preds = %17
  %23 = load %Array*, %Array** %2, align 8
  %24 = getelementptr inbounds %Array, %Array* %23, i32 0, i32 5
  %25 = load i8*, i8** %24, align 8
  %26 = bitcast i8* %25 to i64*
  store i64* %26, i64** %3, align 8
  store i32 0, i32* %4, align 4
  br label %27

27:                                               ; preds = %40, %22
  %28 = load i32, i32* %4, align 4
  %29 = load %Array*, %Array** %2, align 8
  %30 = getelementptr inbounds %Array, %Array* %29, i32 0, i32 0
  %31 = load i32, i32* %30, align 8
  %32 = icmp ult i32 %28, %31
  br i1 %32, label %33, label %43

33:                                               ; preds = %27
  %34 = load i64*, i64** %3, align 8
  %35 = load i32, i32* %4, align 4
  %36 = zext i32 %35 to i64
  %37 = getelementptr inbounds i64, i64* %34, i64 %36
  %38 = load i64, i64* %37, align 8
  %39 = inttoptr i64 %38 to %class.QUBIT*
  call void @__quantum__rt__qubit_release(%class.QUBIT* %39)
  br label %40

40:                                               ; preds = %33
  %41 = load i32, i32* %4, align 4
  %42 = add i32 %41, 1
  store i32 %42, i32* %4, align 4
  br label %27

43:                                               ; preds = %27
  br label %44

44:                                               ; preds = %43, %17
  %45 = load %Array*, %Array** %2, align 8
  call void @__quantum__rt__array_update_reference_count(%Array* %45, i32 -1)
  br label %46

46:                                               ; preds = %44, %7
  ret void
}

declare void @__quantum__rt__qubit_release(%class.QUBIT*) #2

; Function Attrs: noinline optnone ssp uwtable
define void @__quantum__rt__array_update_reference_count(%Array* %0, i32 %1) #0 {
  %3 = alloca %Array*, align 8
  %4 = alloca i32, align 4
  %5 = alloca i32, align 4
  %6 = alloca i32, align 4
  %7 = alloca i64, align 8
  store %Array* %0, %Array** %3, align 8
  store i32 %1, i32* %4, align 4
  %8 = load %Array*, %Array** %3, align 8
  %9 = icmp eq %Array* %8, null
  br i1 %9, label %13, label %10

10:                                               ; preds = %2
  %11 = load i32, i32* %4, align 4
  %12 = icmp eq i32 %11, 0
  br i1 %12, label %13, label %14

13:                                               ; preds = %10, %2
  br label %62

14:                                               ; preds = %10
  %15 = load i32, i32* %4, align 4
  %16 = icmp sgt i32 %15, 0
  br i1 %16, label %17, label %29

17:                                               ; preds = %14
  store i32 0, i32* %5, align 4
  br label %18

18:                                               ; preds = %25, %17
  %19 = load i32, i32* %5, align 4
  %20 = load i32, i32* %4, align 4
  %21 = icmp slt i32 %19, %20
  br i1 %21, label %22, label %28

22:                                               ; preds = %18
  %23 = load %Array*, %Array** %3, align 8
  %24 = call i32 @_ZN8QirArray6AddRefEv(%Array* %23)
  br label %25

25:                                               ; preds = %22
  %26 = load i32, i32* %5, align 4
  %27 = add nsw i32 %26, 1
  store i32 %27, i32* %5, align 4
  br label %18

28:                                               ; preds = %18
  br label %61

29:                                               ; preds = %14
  %30 = load i32, i32* %4, align 4
  store i32 %30, i32* %6, align 4
  br label %31

31:                                               ; preds = %57, %29
  %32 = load i32, i32* %6, align 4
  %33 = icmp slt i32 %32, 0
  br i1 %33, label %34, label %60

34:                                               ; preds = %31
  %35 = load %Array*, %Array** %3, align 8
  %36 = call i32 @_ZN8QirArray7ReleaseEv(%Array* %35)
  %37 = sext i32 %36 to i64
  store i64 %37, i64* %7, align 8
  %38 = load i64, i64* %7, align 8
  %39 = icmp eq i64 %38, 0
  br i1 %39, label %40, label %56

40:                                               ; preds = %34
  %41 = load %Array*, %Array** %3, align 8
  %42 = icmp eq %Array* %41, null
  br i1 %42, label %45, label %43

43:                                               ; preds = %40
  call void @_ZN8QirArrayD1Ev(%Array* %41) #11
  %44 = bitcast %Array* %41 to i8*
  call void @_ZdlPv(i8* %44) #15
  br label %45

45:                                               ; preds = %43, %40
  %46 = load i32, i32* %6, align 4
  %47 = icmp eq i32 %46, -1
  br i1 %47, label %48, label %49

48:                                               ; preds = %45
  br label %49

49:                                               ; preds = %48, %45
  %50 = phi i1 [ false, %45 ], [ true, %48 ]
  %51 = xor i1 %50, true
  br i1 %51, label %52, label %54

52:                                               ; preds = %49
  call void @__assert_rtn(i8* getelementptr inbounds ([44 x i8], [44 x i8]* @__func__.__quantum__rt__array_update_reference_count, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 311, i8* getelementptr inbounds ([65 x i8], [65 x i8]* @.str.21, i64 0, i64 0)) #14
  unreachable

53:                                               ; No predecessors!
  br label %55

54:                                               ; preds = %49
  br label %55

55:                                               ; preds = %54, %53
  br label %60

56:                                               ; preds = %34
  br label %57

57:                                               ; preds = %56
  %58 = load i32, i32* %6, align 4
  %59 = add nsw i32 %58, 1
  store i32 %59, i32* %6, align 4
  br label %31

60:                                               ; preds = %55, %31
  br label %61

61:                                               ; preds = %60, %28
  br label %62

62:                                               ; preds = %13, %61
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define void @__quantum__rt__qubit_return_array(%Array* %0) #0 {
  %2 = alloca %Array*, align 8
  store %Array* %0, %Array** %2, align 8
  %3 = load %Array*, %Array** %2, align 8
  call void @__quantum__rt__qubit_release_array(%Array* %3)
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define %Array* @__quantum__rt__array_create_1d(i32 %0, i64 %1) #0 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %3 = alloca i32, align 4
  %4 = alloca i64, align 8
  %5 = alloca %"class.std::__1::vector", align 8
  %6 = alloca i8*, align 8
  %7 = alloca i32, align 4
  %8 = alloca i1, align 1
  store i32 %0, i32* %3, align 4
  store i64 %1, i64* %4, align 8
  %9 = load i32, i32* %3, align 4
  %10 = icmp sgt i32 %9, 0
  %11 = xor i1 %10, true
  br i1 %11, label %12, label %14

12:                                               ; preds = %2
  call void @__assert_rtn(i8* getelementptr inbounds ([31 x i8], [31 x i8]* @__func__.__quantum__rt__array_create_1d, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 284, i8* getelementptr inbounds ([20 x i8], [20 x i8]* @.str.19, i64 0, i64 0)) #14
  unreachable

13:                                               ; No predecessors!
  br label %15

14:                                               ; preds = %2
  br label %15

15:                                               ; preds = %14, %13
  %16 = call noalias nonnull i8* @_Znwm(i64 64) #16
  store i1 true, i1* %8, align 1
  %17 = bitcast i8* %16 to %Array*
  %18 = load i64, i64* %4, align 8
  %19 = trunc i64 %18 to i32
  %20 = load i32, i32* %3, align 4
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEEC1Ev(%"class.std::__1::vector"* %5) #11
  invoke void @_ZN8QirArrayC1EjjhONSt3__16vectorIjNS0_9allocatorIjEEEE(%Array* %17, i32 %19, i32 %20, i8 zeroext 1, %"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %5)
          to label %21 unwind label %22

21:                                               ; preds = %15
  store i1 false, i1* %8, align 1
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEED1Ev(%"class.std::__1::vector"* %5) #11
  ret %Array* %17

22:                                               ; preds = %15
  %23 = landingpad { i8*, i32 }
          cleanup
  %24 = extractvalue { i8*, i32 } %23, 0
  store i8* %24, i8** %6, align 8
  %25 = extractvalue { i8*, i32 } %23, 1
  store i32 %25, i32* %7, align 4
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEED1Ev(%"class.std::__1::vector"* %5) #11
  %26 = load i1, i1* %8, align 1
  br i1 %26, label %27, label %28

27:                                               ; preds = %22
  call void @_ZdlPv(i8* %16) #15
  br label %28

28:                                               ; preds = %27, %22
  br label %29

29:                                               ; preds = %28
  %30 = load i8*, i8** %6, align 8
  %31 = load i32, i32* %7, align 4
  %32 = insertvalue { i8*, i32 } undef, i8* %30, 0
  %33 = insertvalue { i8*, i32 } %32, i32 %31, 1
  resume { i8*, i32 } %33
}

; Function Attrs: noinline optnone ssp uwtable
define void @__quantum__rt__array_update_alias_count(%Array* %0, i32 %1) #0 {
  %3 = alloca %Array*, align 8
  %4 = alloca i32, align 4
  store %Array* %0, %Array** %3, align 8
  store i32 %1, i32* %4, align 4
  %5 = load %Array*, %Array** %3, align 8
  %6 = icmp eq %Array* %5, null
  br i1 %6, label %10, label %7

7:                                                ; preds = %2
  %8 = load i32, i32* %4, align 4
  %9 = icmp eq i32 %8, 0
  br i1 %9, label %10, label %11

10:                                               ; preds = %7, %2
  br label %23

11:                                               ; preds = %7
  %12 = load i32, i32* %4, align 4
  %13 = load %Array*, %Array** %3, align 8
  %14 = getelementptr inbounds %Array, %Array* %13, i32 0, i32 9
  %15 = load i32, i32* %14, align 8
  %16 = add nsw i32 %15, %12
  store i32 %16, i32* %14, align 8
  %17 = load %Array*, %Array** %3, align 8
  %18 = getelementptr inbounds %Array, %Array* %17, i32 0, i32 9
  %19 = load i32, i32* %18, align 8
  %20 = icmp slt i32 %19, 0
  br i1 %20, label %21, label %23

21:                                               ; preds = %11
  %22 = call %struct.QirString* @__quantum__rt__string_create(i8* getelementptr inbounds ([32 x i8], [32 x i8]* @.str.22, i64 0, i64 0))
  call void @__quantum__rt__fail(%struct.QirString* %22) #18
  unreachable

23:                                               ; preds = %10, %11
  ret void
}

; Function Attrs: noreturn
declare void @__quantum__rt__fail(%struct.QirString*) #10

declare %struct.QirString* @__quantum__rt__string_create(i8*) #2

; Function Attrs: noinline optnone ssp uwtable
define i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %0, i64 %1) #0 {
  %3 = alloca %Array*, align 8
  %4 = alloca i64, align 8
  store %Array* %0, %Array** %3, align 8
  store i64 %1, i64* %4, align 8
  %5 = load %Array*, %Array** %3, align 8
  %6 = icmp ne %Array* %5, null
  %7 = xor i1 %6, true
  br i1 %7, label %8, label %10

8:                                                ; preds = %2
  call void @__assert_rtn(i8* getelementptr inbounds ([40 x i8], [40 x i8]* @__func__.__quantum__rt__array_get_element_ptr_1d, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 334, i8* getelementptr inbounds ([17 x i8], [17 x i8]* @.str.23, i64 0, i64 0)) #14
  unreachable

9:                                                ; No predecessors!
  br label %11

10:                                               ; preds = %2
  br label %11

11:                                               ; preds = %10, %9
  %12 = load %Array*, %Array** %3, align 8
  %13 = load i64, i64* %4, align 8
  %14 = trunc i64 %13 to i32
  %15 = call i8* @_ZNK8QirArray14GetItemPointerEj(%Array* %12, i32 %14)
  ret i8* %15
}

; Function Attrs: noinline optnone ssp uwtable
define i32 @__quantum__rt__array_get_dim(%Array* %0) #0 {
  %2 = alloca %Array*, align 8
  store %Array* %0, %Array** %2, align 8
  %3 = load %Array*, %Array** %2, align 8
  %4 = icmp ne %Array* %3, null
  %5 = xor i1 %4, true
  br i1 %5, label %6, label %8

6:                                                ; preds = %1
  call void @__assert_rtn(i8* getelementptr inbounds ([29 x i8], [29 x i8]* @__func__.__quantum__rt__array_get_dim, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 341, i8* getelementptr inbounds ([17 x i8], [17 x i8]* @.str.23, i64 0, i64 0)) #14
  unreachable

7:                                                ; No predecessors!
  br label %9

8:                                                ; preds = %1
  br label %9

9:                                                ; preds = %8, %7
  %10 = load %Array*, %Array** %2, align 8
  %11 = getelementptr inbounds %Array, %Array* %10, i32 0, i32 2
  %12 = load i8, i8* %11, align 8
  %13 = zext i8 %12 to i32
  ret i32 %13
}

; Function Attrs: noinline optnone ssp uwtable
define i64 @__quantum__rt__array_get_size(%Array* %0, i32 %1) #0 {
  %3 = alloca %Array*, align 8
  %4 = alloca i32, align 4
  store %Array* %0, %Array** %3, align 8
  store i32 %1, i32* %4, align 4
  %5 = load %Array*, %Array** %3, align 8
  %6 = icmp ne %Array* %5, null
  %7 = xor i1 %6, true
  br i1 %7, label %8, label %10

8:                                                ; preds = %2
  call void @__assert_rtn(i8* getelementptr inbounds ([30 x i8], [30 x i8]* @__func__.__quantum__rt__array_get_size, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 348, i8* getelementptr inbounds ([17 x i8], [17 x i8]* @.str.23, i64 0, i64 0)) #14
  unreachable

9:                                                ; No predecessors!
  br label %11

10:                                               ; preds = %2
  br label %11

11:                                               ; preds = %10, %9
  %12 = load i32, i32* %4, align 4
  %13 = load %Array*, %Array** %3, align 8
  %14 = getelementptr inbounds %Array, %Array* %13, i32 0, i32 2
  %15 = load i8, i8* %14, align 8
  %16 = zext i8 %15 to i32
  %17 = icmp slt i32 %12, %16
  %18 = xor i1 %17, true
  br i1 %18, label %19, label %21

19:                                               ; preds = %11
  call void @__assert_rtn(i8* getelementptr inbounds ([30 x i8], [30 x i8]* @__func__.__quantum__rt__array_get_size, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 349, i8* getelementptr inbounds ([24 x i8], [24 x i8]* @.str.24, i64 0, i64 0)) #14
  unreachable

20:                                               ; No predecessors!
  br label %22

21:                                               ; preds = %11
  br label %22

22:                                               ; preds = %21, %20
  %23 = load %Array*, %Array** %3, align 8
  %24 = getelementptr inbounds %Array, %Array* %23, i32 0, i32 4
  %25 = load i32, i32* %4, align 4
  %26 = sext i32 %25 to i64
  %27 = call nonnull align 4 dereferenceable(4) i32* @_ZNSt3__16vectorIjNS_9allocatorIjEEEixEm(%"class.std::__1::vector"* %24, i64 %26) #11
  %28 = load i32, i32* %27, align 4
  %29 = zext i32 %28 to i64
  ret i64 %29
}

; Function Attrs: noinline optnone ssp uwtable
define i64 @__quantum__rt__array_get_size_1d(%Array* %0) #0 {
  %2 = alloca %Array*, align 8
  store %Array* %0, %Array** %2, align 8
  %3 = load %Array*, %Array** %2, align 8
  %4 = call i64 @__quantum__rt__array_get_size(%Array* %3, i32 0)
  ret i64 %4
}

; Function Attrs: noinline optnone ssp uwtable
define %Array* @__quantum__rt__array_copy(%Array* %0, i1 zeroext %1) #0 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %3 = alloca %Array*, align 8
  %4 = alloca %Array*, align 8
  %5 = alloca i8, align 1
  %6 = alloca i8*, align 8
  %7 = alloca i32, align 4
  store %Array* %0, %Array** %4, align 8
  %8 = zext i1 %1 to i8
  store i8 %8, i8* %5, align 1
  %9 = load %Array*, %Array** %4, align 8
  %10 = icmp eq %Array* %9, null
  br i1 %10, label %11, label %12

11:                                               ; preds = %2
  store %Array* null, %Array** %3, align 8
  br label %33

12:                                               ; preds = %2
  %13 = load i8, i8* %5, align 1
  %14 = trunc i8 %13 to i1
  br i1 %14, label %20, label %15

15:                                               ; preds = %12
  %16 = load %Array*, %Array** %4, align 8
  %17 = getelementptr inbounds %Array, %Array* %16, i32 0, i32 9
  %18 = load i32, i32* %17, align 8
  %19 = icmp sgt i32 %18, 0
  br i1 %19, label %20, label %29

20:                                               ; preds = %15, %12
  %21 = call noalias nonnull i8* @_Znwm(i64 64) #16
  %22 = bitcast i8* %21 to %Array*
  %23 = load %Array*, %Array** %4, align 8
  invoke void @_ZN8QirArrayC1ERKS_(%Array* %22, %Array* nonnull align 8 dereferenceable(60) %23)
          to label %24 unwind label %25

24:                                               ; preds = %20
  store %Array* %22, %Array** %3, align 8
  br label %33

25:                                               ; preds = %20
  %26 = landingpad { i8*, i32 }
          cleanup
  %27 = extractvalue { i8*, i32 } %26, 0
  store i8* %27, i8** %6, align 8
  %28 = extractvalue { i8*, i32 } %26, 1
  store i32 %28, i32* %7, align 4
  call void @_ZdlPv(i8* %21) #15
  br label %35

29:                                               ; preds = %15
  %30 = load %Array*, %Array** %4, align 8
  %31 = call i32 @_ZN8QirArray6AddRefEv(%Array* %30)
  %32 = load %Array*, %Array** %4, align 8
  store %Array* %32, %Array** %3, align 8
  br label %33

33:                                               ; preds = %29, %24, %11
  %34 = load %Array*, %Array** %3, align 8
  ret %Array* %34

35:                                               ; preds = %25
  %36 = load i8*, i8** %6, align 8
  %37 = load i32, i32* %7, align 4
  %38 = insertvalue { i8*, i32 } undef, i8* %36, 0
  %39 = insertvalue { i8*, i32 } %38, i32 %37, 1
  resume { i8*, i32 } %39
}

; Function Attrs: noinline optnone ssp uwtable
define %Array* @__quantum__rt__array_concatenate(%Array* %0, %Array* %1) #0 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %3 = alloca %Array*, align 8
  %4 = alloca %Array*, align 8
  %5 = alloca %Array*, align 8
  %6 = alloca i8*, align 8
  %7 = alloca i32, align 4
  store %Array* %0, %Array** %3, align 8
  store %Array* %1, %Array** %4, align 8
  %8 = load %Array*, %Array** %3, align 8
  %9 = icmp ne %Array* %8, null
  br i1 %9, label %10, label %13

10:                                               ; preds = %2
  %11 = load %Array*, %Array** %4, align 8
  %12 = icmp ne %Array* %11, null
  br label %13

13:                                               ; preds = %10, %2
  %14 = phi i1 [ false, %2 ], [ %12, %10 ]
  %15 = xor i1 %14, true
  br i1 %15, label %16, label %18

16:                                               ; preds = %13
  call void @__assert_rtn(i8* getelementptr inbounds ([33 x i8], [33 x i8]* @__func__.__quantum__rt__array_concatenate, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 375, i8* getelementptr inbounds ([35 x i8], [35 x i8]* @.str.25, i64 0, i64 0)) #14
  unreachable

17:                                               ; No predecessors!
  br label %19

18:                                               ; preds = %13
  br label %19

19:                                               ; preds = %18, %17
  %20 = load %Array*, %Array** %3, align 8
  %21 = getelementptr inbounds %Array, %Array* %20, i32 0, i32 2
  %22 = load i8, i8* %21, align 8
  %23 = zext i8 %22 to i32
  %24 = icmp eq i32 %23, 1
  br i1 %24, label %25, label %31

25:                                               ; preds = %19
  %26 = load %Array*, %Array** %4, align 8
  %27 = getelementptr inbounds %Array, %Array* %26, i32 0, i32 2
  %28 = load i8, i8* %27, align 8
  %29 = zext i8 %28 to i32
  %30 = icmp eq i32 %29, 1
  br label %31

31:                                               ; preds = %25, %19
  %32 = phi i1 [ false, %19 ], [ %30, %25 ]
  %33 = xor i1 %32, true
  br i1 %33, label %34, label %36

34:                                               ; preds = %31
  call void @__assert_rtn(i8* getelementptr inbounds ([33 x i8], [33 x i8]* @__func__.__quantum__rt__array_concatenate, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 376, i8* getelementptr inbounds ([47 x i8], [47 x i8]* @.str.26, i64 0, i64 0)) #14
  unreachable

35:                                               ; No predecessors!
  br label %37

36:                                               ; preds = %31
  br label %37

37:                                               ; preds = %36, %35
  %38 = call noalias nonnull i8* @_Znwm(i64 64) #16
  %39 = bitcast i8* %38 to %Array*
  %40 = load %Array*, %Array** %3, align 8
  invoke void @_ZN8QirArrayC1ERKS_(%Array* %39, %Array* nonnull align 8 dereferenceable(60) %40)
          to label %41 unwind label %45

41:                                               ; preds = %37
  store %Array* %39, %Array** %5, align 8
  %42 = load %Array*, %Array** %5, align 8
  %43 = load %Array*, %Array** %4, align 8
  call void @_ZN8QirArray6AppendEPKS_(%Array* %42, %Array* %43)
  %44 = load %Array*, %Array** %5, align 8
  ret %Array* %44

45:                                               ; preds = %37
  %46 = landingpad { i8*, i32 }
          cleanup
  %47 = extractvalue { i8*, i32 } %46, 0
  store i8* %47, i8** %6, align 8
  %48 = extractvalue { i8*, i32 } %46, 1
  store i32 %48, i32* %7, align 4
  call void @_ZdlPv(i8* %38) #15
  br label %49

49:                                               ; preds = %45
  %50 = load i8*, i8** %6, align 8
  %51 = load i32, i32* %7, align 4
  %52 = insertvalue { i8*, i32 } undef, i8* %50, 0
  %53 = insertvalue { i8*, i32 } %52, i32 %51, 1
  resume { i8*, i32 } %53
}

; Function Attrs: noinline optnone ssp uwtable
define %Array* @__quantum__rt__array_create_nonvariadic(i32 %0, i32 %1, %struct.__va_list_tag* %2) #0 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %4 = alloca i32, align 4
  %5 = alloca i32, align 4
  %6 = alloca %struct.__va_list_tag*, align 8
  %7 = alloca %"class.std::__1::vector", align 8
  %8 = alloca i8*, align 8
  %9 = alloca i32, align 4
  %10 = alloca i32, align 4
  %11 = alloca i32, align 4
  %12 = alloca i32, align 4
  store i32 %0, i32* %4, align 4
  store i32 %1, i32* %5, align 4
  store %struct.__va_list_tag* %2, %struct.__va_list_tag** %6, align 8
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEEC1Ev(%"class.std::__1::vector"* %7) #11
  %13 = load i32, i32* %5, align 4
  %14 = sext i32 %13 to i64
  invoke void @_ZNSt3__16vectorIjNS_9allocatorIjEEE7reserveEm(%"class.std::__1::vector"* %7, i64 %14)
          to label %15 unwind label %31

15:                                               ; preds = %3
  store i32 1, i32* %10, align 4
  store i32 0, i32* %11, align 4
  br label %16

16:                                               ; preds = %48, %15
  %17 = load i32, i32* %11, align 4
  %18 = load i32, i32* %5, align 4
  %19 = icmp slt i32 %17, %18
  br i1 %19, label %20, label %51

20:                                               ; preds = %16
  %21 = load %struct.__va_list_tag*, %struct.__va_list_tag** %6, align 8
  %22 = getelementptr inbounds %struct.__va_list_tag, %struct.__va_list_tag* %21, i32 0, i32 0
  %23 = load i32, i32* %22, align 8
  %24 = icmp ule i32 %23, 40
  br i1 %24, label %25, label %35

25:                                               ; preds = %20
  %26 = getelementptr inbounds %struct.__va_list_tag, %struct.__va_list_tag* %21, i32 0, i32 3
  %27 = load i8*, i8** %26, align 8
  %28 = getelementptr i8, i8* %27, i32 %23
  %29 = bitcast i8* %28 to i64*
  %30 = add i32 %23, 8
  store i32 %30, i32* %22, align 8
  br label %40

31:                                               ; preds = %61, %57, %40, %3
  %32 = landingpad { i8*, i32 }
          cleanup
  %33 = extractvalue { i8*, i32 } %32, 0
  store i8* %33, i8** %8, align 8
  %34 = extractvalue { i8*, i32 } %32, 1
  store i32 %34, i32* %9, align 4
  br label %75

35:                                               ; preds = %20
  %36 = getelementptr inbounds %struct.__va_list_tag, %struct.__va_list_tag* %21, i32 0, i32 2
  %37 = load i8*, i8** %36, align 8
  %38 = bitcast i8* %37 to i64*
  %39 = getelementptr i8, i8* %37, i32 8
  store i8* %39, i8** %36, align 8
  br label %40

40:                                               ; preds = %35, %25
  %41 = phi i64* [ %29, %25 ], [ %38, %35 ]
  %42 = load i64, i64* %41, align 8
  %43 = trunc i64 %42 to i32
  store i32 %43, i32* %12, align 4
  invoke void @_ZNSt3__16vectorIjNS_9allocatorIjEEE9push_backERKj(%"class.std::__1::vector"* %7, i32* nonnull align 4 dereferenceable(4) %12)
          to label %44 unwind label %31

44:                                               ; preds = %40
  %45 = load i32, i32* %12, align 4
  %46 = load i32, i32* %10, align 4
  %47 = mul i32 %46, %45
  store i32 %47, i32* %10, align 4
  br label %48

48:                                               ; preds = %44
  %49 = load i32, i32* %11, align 4
  %50 = add nsw i32 %49, 1
  store i32 %50, i32* %11, align 4
  br label %16

51:                                               ; preds = %16
  %52 = load i32, i32* %5, align 4
  %53 = call zeroext i8 @_ZNSt3__114numeric_limitsIhE3maxEv() #11
  %54 = zext i8 %53 to i32
  %55 = icmp slt i32 %52, %54
  %56 = xor i1 %55, true
  br i1 %56, label %57, label %60

57:                                               ; preds = %51
  invoke void @__assert_rtn(i8* getelementptr inbounds ([40 x i8], [40 x i8]* @__func__.__quantum__rt__array_create_nonvariadic, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 401, i8* getelementptr inbounds ([66 x i8], [66 x i8]* @.str.27, i64 0, i64 0)) #14
          to label %58 unwind label %31

58:                                               ; preds = %57
  unreachable

59:                                               ; No predecessors!
  br label %61

60:                                               ; preds = %51
  br label %61

61:                                               ; preds = %60, %59
  %62 = invoke noalias nonnull i8* @_Znwm(i64 64) #16
          to label %63 unwind label %31

63:                                               ; preds = %61
  %64 = bitcast i8* %62 to %Array*
  %65 = load i32, i32* %10, align 4
  %66 = load i32, i32* %4, align 4
  %67 = load i32, i32* %5, align 4
  %68 = trunc i32 %67 to i8
  %69 = call nonnull align 8 dereferenceable(24) %"class.std::__1::vector"* @_ZNSt3__14moveIRNS_6vectorIjNS_9allocatorIjEEEEEEONS_16remove_referenceIT_E4typeEOS7_(%"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %7) #11
  invoke void @_ZN8QirArrayC1EjjhONSt3__16vectorIjNS0_9allocatorIjEEEE(%Array* %64, i32 %65, i32 %66, i8 zeroext %68, %"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %69)
          to label %70 unwind label %71

70:                                               ; preds = %63
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEED1Ev(%"class.std::__1::vector"* %7) #11
  ret %Array* %64

71:                                               ; preds = %63
  %72 = landingpad { i8*, i32 }
          cleanup
  %73 = extractvalue { i8*, i32 } %72, 0
  store i8* %73, i8** %8, align 8
  %74 = extractvalue { i8*, i32 } %72, 1
  store i32 %74, i32* %9, align 4
  call void @_ZdlPv(i8* %62) #15
  br label %75

75:                                               ; preds = %71, %31
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEED1Ev(%"class.std::__1::vector"* %7) #11
  br label %76

76:                                               ; preds = %75
  %77 = load i8*, i8** %8, align 8
  %78 = load i32, i32* %9, align 4
  %79 = insertvalue { i8*, i32 } undef, i8* %77, 0
  %80 = insertvalue { i8*, i32 } %79, i32 %78, 1
  resume { i8*, i32 } %80
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__16vectorIjNS_9allocatorIjEEE7reserveEm(%"class.std::__1::vector"* %0, i64 %1) #0 align 2 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %3 = alloca %"class.std::__1::vector"*, align 8
  %4 = alloca i64, align 8
  %5 = alloca %"class.std::__1::allocator"*, align 8
  %6 = alloca %"struct.std::__1::__split_buffer", align 8
  %7 = alloca i8*, align 8
  %8 = alloca i32, align 4
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %3, align 8
  store i64 %1, i64* %4, align 8
  %9 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  %10 = load i64, i64* %4, align 8
  %11 = call i64 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE8capacityEv(%"class.std::__1::vector"* %9) #11
  %12 = icmp ugt i64 %10, %11
  br i1 %12, label %13, label %24

13:                                               ; preds = %2
  %14 = bitcast %"class.std::__1::vector"* %9 to %"class.std::__1::__vector_base"*
  %15 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEE7__allocEv(%"class.std::__1::__vector_base"* %14) #11
  store %"class.std::__1::allocator"* %15, %"class.std::__1::allocator"** %5, align 8
  %16 = load i64, i64* %4, align 8
  %17 = call i64 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE4sizeEv(%"class.std::__1::vector"* %9) #11
  %18 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %5, align 8
  call void @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEEC1EmmS3_(%"struct.std::__1::__split_buffer"* %6, i64 %16, i64 %17, %"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %18)
  invoke void @_ZNSt3__16vectorIjNS_9allocatorIjEEE26__swap_out_circular_bufferERNS_14__split_bufferIjRS2_EE(%"class.std::__1::vector"* %9, %"struct.std::__1::__split_buffer"* nonnull align 8 dereferenceable(40) %6)
          to label %19 unwind label %20

19:                                               ; preds = %13
  call void @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEED1Ev(%"struct.std::__1::__split_buffer"* %6) #11
  br label %24

20:                                               ; preds = %13
  %21 = landingpad { i8*, i32 }
          cleanup
  %22 = extractvalue { i8*, i32 } %21, 0
  store i8* %22, i8** %7, align 8
  %23 = extractvalue { i8*, i32 } %21, 1
  store i32 %23, i32* %8, align 4
  call void @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEED1Ev(%"struct.std::__1::__split_buffer"* %6) #11
  br label %25

24:                                               ; preds = %19, %2
  ret void

25:                                               ; preds = %20
  %26 = load i8*, i8** %7, align 8
  %27 = load i32, i32* %8, align 4
  %28 = insertvalue { i8*, i32 } undef, i8* %26, 0
  %29 = insertvalue { i8*, i32 } %28, i32 %27, 1
  resume { i8*, i32 } %29
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden zeroext i8 @_ZNSt3__114numeric_limitsIhE3maxEv() #1 align 2 {
  %1 = call zeroext i8 @_ZNSt3__123__libcpp_numeric_limitsIhLb1EE3maxEv() #11
  ret i8 %1
}

; Function Attrs: noinline optnone ssp uwtable
define %Array* @__quantum__rt__array_create(i32 %0, i32 %1, ...) #0 {
  %3 = alloca i32, align 4
  %4 = alloca i32, align 4
  %5 = alloca [1 x %struct.__va_list_tag], align 16
  %6 = alloca %Array*, align 8
  store i32 %0, i32* %3, align 4
  store i32 %1, i32* %4, align 4
  %7 = getelementptr inbounds [1 x %struct.__va_list_tag], [1 x %struct.__va_list_tag]* %5, i64 0, i64 0
  %8 = bitcast %struct.__va_list_tag* %7 to i8*
  call void @llvm.va_start(i8* %8)
  %9 = load i32, i32* %3, align 4
  %10 = load i32, i32* %4, align 4
  %11 = getelementptr inbounds [1 x %struct.__va_list_tag], [1 x %struct.__va_list_tag]* %5, i64 0, i64 0
  %12 = call %Array* @__quantum__rt__array_create_nonvariadic(i32 %9, i32 %10, %struct.__va_list_tag* %11)
  store %Array* %12, %Array** %6, align 8
  %13 = getelementptr inbounds [1 x %struct.__va_list_tag], [1 x %struct.__va_list_tag]* %5, i64 0, i64 0
  %14 = bitcast %struct.__va_list_tag* %13 to i8*
  call void @llvm.va_end(i8* %14)
  %15 = load %Array*, %Array** %6, align 8
  ret %Array* %15
}

; Function Attrs: nounwind
declare void @llvm.va_start(i8*) #11

; Function Attrs: nounwind
declare void @llvm.va_end(i8*) #11

; Function Attrs: noinline optnone ssp uwtable
define i8* @__quantum__rt__array_get_element_ptr_nonvariadic(%Array* %0, %struct.__va_list_tag* %1) #0 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %3 = alloca %Array*, align 8
  %4 = alloca %struct.__va_list_tag*, align 8
  %5 = alloca %"class.std::__1::vector", align 8
  %6 = alloca i8*, align 8
  %7 = alloca i32, align 4
  %8 = alloca i8, align 1
  %9 = alloca i32, align 4
  %10 = alloca i32, align 4
  store %Array* %0, %Array** %3, align 8
  store %struct.__va_list_tag* %1, %struct.__va_list_tag** %4, align 8
  %11 = load %Array*, %Array** %3, align 8
  %12 = icmp ne %Array* %11, null
  %13 = xor i1 %12, true
  br i1 %13, label %14, label %16

14:                                               ; preds = %2
  call void @__assert_rtn(i8* getelementptr inbounds ([49 x i8], [49 x i8]* @__func__.__quantum__rt__array_get_element_ptr_nonvariadic, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 420, i8* getelementptr inbounds ([17 x i8], [17 x i8]* @.str.23, i64 0, i64 0)) #14
  unreachable

15:                                               ; No predecessors!
  br label %17

16:                                               ; preds = %2
  br label %17

17:                                               ; preds = %16, %15
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEEC1Ev(%"class.std::__1::vector"* %5) #11
  %18 = load %Array*, %Array** %3, align 8
  %19 = getelementptr inbounds %Array, %Array* %18, i32 0, i32 2
  %20 = load i8, i8* %19, align 8
  %21 = zext i8 %20 to i64
  invoke void @_ZNSt3__16vectorIjNS_9allocatorIjEEE7reserveEm(%"class.std::__1::vector"* %5, i64 %21)
          to label %22 unwind label %42

22:                                               ; preds = %17
  store i8 0, i8* %8, align 1
  br label %23

23:                                               ; preds = %71, %22
  %24 = load i8, i8* %8, align 1
  %25 = zext i8 %24 to i32
  %26 = load %Array*, %Array** %3, align 8
  %27 = getelementptr inbounds %Array, %Array* %26, i32 0, i32 2
  %28 = load i8, i8* %27, align 8
  %29 = zext i8 %28 to i32
  %30 = icmp slt i32 %25, %29
  br i1 %30, label %31, label %74

31:                                               ; preds = %23
  %32 = load %struct.__va_list_tag*, %struct.__va_list_tag** %4, align 8
  %33 = getelementptr inbounds %struct.__va_list_tag, %struct.__va_list_tag* %32, i32 0, i32 0
  %34 = load i32, i32* %33, align 8
  %35 = icmp ule i32 %34, 40
  br i1 %35, label %36, label %46

36:                                               ; preds = %31
  %37 = getelementptr inbounds %struct.__va_list_tag, %struct.__va_list_tag* %32, i32 0, i32 3
  %38 = load i8*, i8** %37, align 8
  %39 = getelementptr i8, i8* %38, i32 %34
  %40 = bitcast i8* %39 to i64*
  %41 = add i32 %34, 8
  store i32 %41, i32* %33, align 8
  br label %51

42:                                               ; preds = %78, %74, %66, %51, %17
  %43 = landingpad { i8*, i32 }
          cleanup
  %44 = extractvalue { i8*, i32 } %43, 0
  store i8* %44, i8** %6, align 8
  %45 = extractvalue { i8*, i32 } %43, 1
  store i32 %45, i32* %7, align 4
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEED1Ev(%"class.std::__1::vector"* %5) #11
  br label %83

46:                                               ; preds = %31
  %47 = getelementptr inbounds %struct.__va_list_tag, %struct.__va_list_tag* %32, i32 0, i32 2
  %48 = load i8*, i8** %47, align 8
  %49 = bitcast i8* %48 to i64*
  %50 = getelementptr i8, i8* %48, i32 8
  store i8* %50, i8** %47, align 8
  br label %51

51:                                               ; preds = %46, %36
  %52 = phi i64* [ %40, %36 ], [ %49, %46 ]
  %53 = load i64, i64* %52, align 8
  %54 = trunc i64 %53 to i32
  store i32 %54, i32* %9, align 4
  invoke void @_ZNSt3__16vectorIjNS_9allocatorIjEEE9push_backEOj(%"class.std::__1::vector"* %5, i32* nonnull align 4 dereferenceable(4) %9)
          to label %55 unwind label %42

55:                                               ; preds = %51
  %56 = call nonnull align 4 dereferenceable(4) i32* @_ZNSt3__16vectorIjNS_9allocatorIjEEE4backEv(%"class.std::__1::vector"* %5) #11
  %57 = load i32, i32* %56, align 4
  %58 = load %Array*, %Array** %3, align 8
  %59 = getelementptr inbounds %Array, %Array* %58, i32 0, i32 4
  %60 = load i8, i8* %8, align 1
  %61 = zext i8 %60 to i64
  %62 = call nonnull align 4 dereferenceable(4) i32* @_ZNSt3__16vectorIjNS_9allocatorIjEEEixEm(%"class.std::__1::vector"* %59, i64 %61) #11
  %63 = load i32, i32* %62, align 4
  %64 = icmp ult i32 %57, %63
  %65 = xor i1 %64, true
  br i1 %65, label %66, label %69

66:                                               ; preds = %55
  invoke void @__assert_rtn(i8* getelementptr inbounds ([49 x i8], [49 x i8]* @__func__.__quantum__rt__array_get_element_ptr_nonvariadic, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 429, i8* getelementptr inbounds ([42 x i8], [42 x i8]* @.str.28, i64 0, i64 0)) #14
          to label %67 unwind label %42

67:                                               ; preds = %66
  unreachable

68:                                               ; No predecessors!
  br label %70

69:                                               ; preds = %55
  br label %70

70:                                               ; preds = %69, %68
  br label %71

71:                                               ; preds = %70
  %72 = load i8, i8* %8, align 1
  %73 = add i8 %72, 1
  store i8 %73, i8* %8, align 1
  br label %23

74:                                               ; preds = %23
  %75 = load %Array*, %Array** %3, align 8
  %76 = getelementptr inbounds %Array, %Array* %75, i32 0, i32 4
  %77 = invoke i32 @_ZL14GetLinearIndexRKNSt3__16vectorIjNS_9allocatorIjEEEES5_(%"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %76, %"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %5)
          to label %78 unwind label %42

78:                                               ; preds = %74
  store i32 %77, i32* %10, align 4
  %79 = load %Array*, %Array** %3, align 8
  %80 = load i32, i32* %10, align 4
  %81 = invoke i8* @_ZNK8QirArray14GetItemPointerEj(%Array* %79, i32 %80)
          to label %82 unwind label %42

82:                                               ; preds = %78
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEED1Ev(%"class.std::__1::vector"* %5) #11
  ret i8* %81

83:                                               ; preds = %42
  %84 = load i8*, i8** %6, align 8
  %85 = load i32, i32* %7, align 4
  %86 = insertvalue { i8*, i32 } undef, i8* %84, 0
  %87 = insertvalue { i8*, i32 } %86, i32 %85, 1
  resume { i8*, i32 } %87
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__16vectorIjNS_9allocatorIjEEE9push_backEOj(%"class.std::__1::vector"* %0, i32* nonnull align 4 dereferenceable(4) %1) #0 align 2 {
  %3 = alloca %"class.std::__1::vector"*, align 8
  %4 = alloca i32*, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %3, align 8
  store i32* %1, i32** %4, align 8
  %5 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  %6 = bitcast %"class.std::__1::vector"* %5 to %"class.std::__1::__vector_base"*
  %7 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %6, i32 0, i32 1
  %8 = load i32*, i32** %7, align 8
  %9 = bitcast %"class.std::__1::vector"* %5 to %"class.std::__1::__vector_base"*
  %10 = call nonnull align 8 dereferenceable(8) i32** @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEE9__end_capEv(%"class.std::__1::__vector_base"* %9) #11
  %11 = load i32*, i32** %10, align 8
  %12 = icmp ult i32* %8, %11
  br i1 %12, label %13, label %16

13:                                               ; preds = %2
  %14 = load i32*, i32** %4, align 8
  %15 = call nonnull align 4 dereferenceable(4) i32* @_ZNSt3__14moveIRjEEONS_16remove_referenceIT_E4typeEOS3_(i32* nonnull align 4 dereferenceable(4) %14) #11
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEE22__construct_one_at_endIJjEEEvDpOT_(%"class.std::__1::vector"* %5, i32* nonnull align 4 dereferenceable(4) %15)
  br label %19

16:                                               ; preds = %2
  %17 = load i32*, i32** %4, align 8
  %18 = call nonnull align 4 dereferenceable(4) i32* @_ZNSt3__14moveIRjEEONS_16remove_referenceIT_E4typeEOS3_(i32* nonnull align 4 dereferenceable(4) %17) #11
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEE21__push_back_slow_pathIjEEvOT_(%"class.std::__1::vector"* %5, i32* nonnull align 4 dereferenceable(4) %18)
  br label %19

19:                                               ; preds = %16, %13
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 4 dereferenceable(4) i32* @_ZNSt3__16vectorIjNS_9allocatorIjEEE4backEv(%"class.std::__1::vector"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::vector"*, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %2, align 8
  %3 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %2, align 8
  %4 = bitcast %"class.std::__1::vector"* %3 to %"class.std::__1::__vector_base"*
  %5 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %4, i32 0, i32 1
  %6 = load i32*, i32** %5, align 8
  %7 = getelementptr inbounds i32, i32* %6, i64 -1
  ret i32* %7
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define internal i32 @_ZL14GetLinearIndexRKNSt3__16vectorIjNS_9allocatorIjEEEES5_(%"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %0, %"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %1) #1 {
  %3 = alloca %"class.std::__1::vector"*, align 8
  %4 = alloca %"class.std::__1::vector"*, align 8
  %5 = alloca i64, align 8
  %6 = alloca i32, align 4
  %7 = alloca i32, align 4
  %8 = alloca i64, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %3, align 8
  store %"class.std::__1::vector"* %1, %"class.std::__1::vector"** %4, align 8
  %9 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  %10 = call i64 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE4sizeEv(%"class.std::__1::vector"* %9) #11
  store i64 %10, i64* %5, align 8
  store i32 0, i32* %6, align 4
  store i32 1, i32* %7, align 4
  %11 = load i64, i64* %5, align 8
  store i64 %11, i64* %8, align 8
  br label %12

12:                                               ; preds = %15, %2
  %13 = load i64, i64* %8, align 8
  %14 = icmp ugt i64 %13, 0
  br i1 %14, label %15, label %32

15:                                               ; preds = %12
  %16 = load i64, i64* %8, align 8
  %17 = add i64 %16, -1
  store i64 %17, i64* %8, align 8
  %18 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %4, align 8
  %19 = load i64, i64* %8, align 8
  %20 = call nonnull align 4 dereferenceable(4) i32* @_ZNKSt3__16vectorIjNS_9allocatorIjEEEixEm(%"class.std::__1::vector"* %18, i64 %19) #11
  %21 = load i32, i32* %20, align 4
  %22 = load i32, i32* %7, align 4
  %23 = mul i32 %21, %22
  %24 = load i32, i32* %6, align 4
  %25 = add i32 %24, %23
  store i32 %25, i32* %6, align 4
  %26 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  %27 = load i64, i64* %8, align 8
  %28 = call nonnull align 4 dereferenceable(4) i32* @_ZNKSt3__16vectorIjNS_9allocatorIjEEEixEm(%"class.std::__1::vector"* %26, i64 %27) #11
  %29 = load i32, i32* %28, align 4
  %30 = load i32, i32* %7, align 4
  %31 = mul i32 %30, %29
  store i32 %31, i32* %7, align 4
  br label %12

32:                                               ; preds = %12
  %33 = load i32, i32* %6, align 4
  ret i32 %33
}

; Function Attrs: noinline optnone ssp uwtable
define i8* @__quantum__rt__array_get_element_ptr(%Array* %0, ...) #0 {
  %2 = alloca %Array*, align 8
  %3 = alloca [1 x %struct.__va_list_tag], align 16
  %4 = alloca i8*, align 8
  store %Array* %0, %Array** %2, align 8
  %5 = load %Array*, %Array** %2, align 8
  %6 = icmp ne %Array* %5, null
  %7 = xor i1 %6, true
  br i1 %7, label %8, label %10

8:                                                ; preds = %1
  call void @__assert_rtn(i8* getelementptr inbounds ([37 x i8], [37 x i8]* @__func__.__quantum__rt__array_get_element_ptr, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 442, i8* getelementptr inbounds ([17 x i8], [17 x i8]* @.str.23, i64 0, i64 0)) #14
  unreachable

9:                                                ; No predecessors!
  br label %11

10:                                               ; preds = %1
  br label %11

11:                                               ; preds = %10, %9
  %12 = getelementptr inbounds [1 x %struct.__va_list_tag], [1 x %struct.__va_list_tag]* %3, i64 0, i64 0
  %13 = bitcast %struct.__va_list_tag* %12 to i8*
  call void @llvm.va_start(i8* %13)
  %14 = load %Array*, %Array** %2, align 8
  %15 = getelementptr inbounds [1 x %struct.__va_list_tag], [1 x %struct.__va_list_tag]* %3, i64 0, i64 0
  %16 = call i8* @__quantum__rt__array_get_element_ptr_nonvariadic(%Array* %14, %struct.__va_list_tag* %15)
  store i8* %16, i8** %4, align 8
  %17 = getelementptr inbounds [1 x %struct.__va_list_tag], [1 x %struct.__va_list_tag]* %3, i64 0, i64 0
  %18 = bitcast %struct.__va_list_tag* %17 to i8*
  call void @llvm.va_end(i8* %18)
  %19 = load i8*, i8** %4, align 8
  ret i8* %19
}

; Function Attrs: noinline optnone ssp uwtable
define %Array* @quantum__rt__array_slice(%Array* %0, i32 %1, %struct.QirRange* nonnull align 8 dereferenceable(24) %2, i1 zeroext %3) #0 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %5 = alloca %Array*, align 8
  %6 = alloca %Array*, align 8
  %7 = alloca i32, align 4
  %8 = alloca %struct.QirRange*, align 8
  %9 = alloca i8, align 1
  %10 = alloca i32, align 4
  %11 = alloca i8, align 1
  %12 = alloca %struct.CheckedRange, align 8
  %13 = alloca %"class.std::__1::vector", align 8
  %14 = alloca i8*, align 8
  %15 = alloca i32, align 4
  %16 = alloca %"class.std::__1::vector", align 8
  %17 = alloca i32, align 4
  %18 = alloca %"class.std::__1::__wrap_iter", align 8
  %19 = alloca %"class.std::__1::__wrap_iter", align 8
  %20 = alloca %"struct.std::__1::multiplies", align 1
  %21 = alloca %Array*, align 8
  %22 = alloca i32, align 4
  %23 = alloca i32, align 4
  %24 = alloca i32, align 4
  %25 = alloca i32, align 4
  %26 = alloca i64, align 8
  %27 = alloca i32, align 4
  %28 = alloca i32, align 4
  %29 = alloca i64, align 8
  %30 = alloca i32, align 4
  %31 = alloca i32, align 4
  %32 = alloca i64, align 8
  %33 = alloca i64, align 8
  store %Array* %0, %Array** %6, align 8
  store i32 %1, i32* %7, align 4
  store %struct.QirRange* %2, %struct.QirRange** %8, align 8
  %34 = zext i1 %3 to i8
  store i8 %34, i8* %9, align 1
  %35 = load %Array*, %Array** %6, align 8
  %36 = icmp ne %Array* %35, null
  %37 = xor i1 %36, true
  br i1 %37, label %38, label %40

38:                                               ; preds = %4
  call void @__assert_rtn(i8* getelementptr inbounds ([25 x i8], [25 x i8]* @__func__.quantum__rt__array_slice, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 543, i8* getelementptr inbounds ([17 x i8], [17 x i8]* @.str.23, i64 0, i64 0)) #14
  unreachable

39:                                               ; No predecessors!
  br label %41

40:                                               ; preds = %4
  br label %41

41:                                               ; preds = %40, %39
  %42 = load i32, i32* %7, align 4
  %43 = icmp sge i32 %42, 0
  br i1 %43, label %44, label %51

44:                                               ; preds = %41
  %45 = load i32, i32* %7, align 4
  %46 = load %Array*, %Array** %6, align 8
  %47 = getelementptr inbounds %Array, %Array* %46, i32 0, i32 2
  %48 = load i8, i8* %47, align 8
  %49 = zext i8 %48 to i32
  %50 = icmp slt i32 %45, %49
  br label %51

51:                                               ; preds = %44, %41
  %52 = phi i1 [ false, %41 ], [ %50, %44 ]
  %53 = xor i1 %52, true
  br i1 %53, label %54, label %56

54:                                               ; preds = %51
  call void @__assert_rtn(i8* getelementptr inbounds ([25 x i8], [25 x i8]* @__func__.quantum__rt__array_slice, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 544, i8* getelementptr inbounds ([36 x i8], [36 x i8]* @.str.29, i64 0, i64 0)) #14
  unreachable

55:                                               ; No predecessors!
  br label %57

56:                                               ; preds = %51
  br label %57

57:                                               ; preds = %56, %55
  %58 = load %Array*, %Array** %6, align 8
  %59 = getelementptr inbounds %Array, %Array* %58, i32 0, i32 1
  %60 = load i32, i32* %59, align 4
  store i32 %60, i32* %10, align 4
  %61 = load %Array*, %Array** %6, align 8
  %62 = getelementptr inbounds %Array, %Array* %61, i32 0, i32 2
  %63 = load i8, i8* %62, align 8
  store i8 %63, i8* %11, align 1
  %64 = load %struct.QirRange*, %struct.QirRange** %8, align 8
  %65 = load %Array*, %Array** %6, align 8
  %66 = getelementptr inbounds %Array, %Array* %65, i32 0, i32 4
  %67 = load i32, i32* %7, align 4
  %68 = sext i32 %67 to i64
  %69 = call nonnull align 4 dereferenceable(4) i32* @_ZNSt3__16vectorIjNS_9allocatorIjEEEixEm(%"class.std::__1::vector"* %66, i64 %68) #11
  %70 = load i32, i32* %69, align 4
  %71 = zext i32 %70 to i64
  call void @_ZN12CheckedRangeC1ERK8QirRangex(%struct.CheckedRange* %12, %struct.QirRange* nonnull align 8 dereferenceable(24) %64, i64 %71)
  %72 = call zeroext i1 @_ZNK12CheckedRange7IsEmptyEv(%struct.CheckedRange* %12)
  br i1 %72, label %73, label %95

73:                                               ; preds = %57
  %74 = load %Array*, %Array** %6, align 8
  %75 = getelementptr inbounds %Array, %Array* %74, i32 0, i32 4
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEEC1ERKS3_(%"class.std::__1::vector"* %13, %"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %75)
  %76 = load i32, i32* %7, align 4
  %77 = sext i32 %76 to i64
  %78 = call nonnull align 4 dereferenceable(4) i32* @_ZNSt3__16vectorIjNS_9allocatorIjEEEixEm(%"class.std::__1::vector"* %13, i64 %77) #11
  store i32 0, i32* %78, align 4
  %79 = invoke noalias nonnull i8* @_Znwm(i64 64) #16
          to label %80 unwind label %86

80:                                               ; preds = %73
  %81 = bitcast i8* %79 to %Array*
  %82 = load i32, i32* %10, align 4
  %83 = load i8, i8* %11, align 1
  %84 = call nonnull align 8 dereferenceable(24) %"class.std::__1::vector"* @_ZNSt3__14moveIRNS_6vectorIjNS_9allocatorIjEEEEEEONS_16remove_referenceIT_E4typeEOS7_(%"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %13) #11
  invoke void @_ZN8QirArrayC1EjjhONSt3__16vectorIjNS0_9allocatorIjEEEE(%Array* %81, i32 0, i32 %82, i8 zeroext %83, %"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %84)
          to label %85 unwind label %90

85:                                               ; preds = %80
  store %Array* %81, %Array** %5, align 8
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEED1Ev(%"class.std::__1::vector"* %13) #11
  br label %394

86:                                               ; preds = %73
  %87 = landingpad { i8*, i32 }
          cleanup
  %88 = extractvalue { i8*, i32 } %87, 0
  store i8* %88, i8** %14, align 8
  %89 = extractvalue { i8*, i32 } %87, 1
  store i32 %89, i32* %15, align 4
  br label %94

90:                                               ; preds = %80
  %91 = landingpad { i8*, i32 }
          cleanup
  %92 = extractvalue { i8*, i32 } %91, 0
  store i8* %92, i8** %14, align 8
  %93 = extractvalue { i8*, i32 } %91, 1
  store i32 %93, i32* %15, align 4
  call void @_ZdlPv(i8* %79) #15
  br label %94

94:                                               ; preds = %90, %86
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEED1Ev(%"class.std::__1::vector"* %13) #11
  br label %396

95:                                               ; preds = %57
  %96 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %12, i32 0, i32 1
  %97 = load i64, i64* %96, align 8
  %98 = icmp eq i64 %97, 1
  br i1 %98, label %99, label %117

99:                                               ; preds = %95
  %100 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %12, i32 0, i32 0
  %101 = load i64, i64* %100, align 8
  %102 = icmp eq i64 %101, 0
  br i1 %102, label %103, label %117

103:                                              ; preds = %99
  %104 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %12, i32 0, i32 2
  %105 = load i64, i64* %104, align 8
  %106 = load %Array*, %Array** %6, align 8
  %107 = getelementptr inbounds %Array, %Array* %106, i32 0, i32 4
  %108 = load i32, i32* %7, align 4
  %109 = sext i32 %108 to i64
  %110 = call nonnull align 4 dereferenceable(4) i32* @_ZNSt3__16vectorIjNS_9allocatorIjEEEixEm(%"class.std::__1::vector"* %107, i64 %109) #11
  %111 = load i32, i32* %110, align 4
  %112 = zext i32 %111 to i64
  %113 = icmp eq i64 %105, %112
  br i1 %113, label %114, label %117

114:                                              ; preds = %103
  %115 = load %Array*, %Array** %6, align 8
  %116 = call %Array* @__quantum__rt__array_copy(%Array* %115, i1 zeroext true)
  store %Array* %116, %Array** %5, align 8
  br label %394

117:                                              ; preds = %103, %99, %95
  %118 = load %Array*, %Array** %6, align 8
  %119 = getelementptr inbounds %Array, %Array* %118, i32 0, i32 4
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEEC1ERKS3_(%"class.std::__1::vector"* %16, %"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %119)
  %120 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %12, i32 0, i32 3
  %121 = load i64, i64* %120, align 8
  %122 = trunc i64 %121 to i32
  %123 = load i32, i32* %7, align 4
  %124 = sext i32 %123 to i64
  %125 = call nonnull align 4 dereferenceable(4) i32* @_ZNSt3__16vectorIjNS_9allocatorIjEEEixEm(%"class.std::__1::vector"* %16, i64 %124) #11
  store i32 %122, i32* %125, align 4
  %126 = call i32* @_ZNSt3__16vectorIjNS_9allocatorIjEEE5beginEv(%"class.std::__1::vector"* %16) #11
  %127 = getelementptr inbounds %"class.std::__1::__wrap_iter", %"class.std::__1::__wrap_iter"* %18, i32 0, i32 0
  store i32* %126, i32** %127, align 8
  %128 = call i32* @_ZNSt3__16vectorIjNS_9allocatorIjEEE3endEv(%"class.std::__1::vector"* %16) #11
  %129 = getelementptr inbounds %"class.std::__1::__wrap_iter", %"class.std::__1::__wrap_iter"* %19, i32 0, i32 0
  store i32* %128, i32** %129, align 8
  %130 = getelementptr inbounds %"class.std::__1::__wrap_iter", %"class.std::__1::__wrap_iter"* %18, i32 0, i32 0
  %131 = load i32*, i32** %130, align 8
  %132 = getelementptr inbounds %"class.std::__1::__wrap_iter", %"class.std::__1::__wrap_iter"* %19, i32 0, i32 0
  %133 = load i32*, i32** %132, align 8
  %134 = invoke i32 @_ZNSt3__110accumulateINS_11__wrap_iterIPjEEjNS_10multipliesIjEEEET0_T_S7_S6_T1_(i32* %131, i32* %133, i32 1)
          to label %135 unwind label %150

135:                                              ; preds = %117
  store i32 %134, i32* %17, align 4
  %136 = invoke noalias nonnull i8* @_Znwm(i64 64) #16
          to label %137 unwind label %150

137:                                              ; preds = %135
  %138 = bitcast i8* %136 to %Array*
  %139 = load i32, i32* %17, align 4
  %140 = load i32, i32* %10, align 4
  %141 = load i8, i8* %11, align 1
  %142 = call nonnull align 8 dereferenceable(24) %"class.std::__1::vector"* @_ZNSt3__14moveIRNS_6vectorIjNS_9allocatorIjEEEEEEONS_16remove_referenceIT_E4typeEOS7_(%"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %16) #11
  invoke void @_ZN8QirArrayC1EjjhONSt3__16vectorIjNS0_9allocatorIjEEEE(%Array* %138, i32 %139, i32 %140, i8 zeroext %141, %"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %142)
          to label %143 unwind label %154

143:                                              ; preds = %137
  store %Array* %138, %Array** %21, align 8
  %144 = load %Array*, %Array** %21, align 8
  %145 = getelementptr inbounds %Array, %Array* %144, i32 0, i32 5
  %146 = load i8*, i8** %145, align 8
  %147 = icmp eq i8* null, %146
  br i1 %147, label %148, label %158

148:                                              ; preds = %143
  %149 = load %Array*, %Array** %21, align 8
  store %Array* %149, %Array** %5, align 8
  store i32 1, i32* %22, align 4
  br label %392

150:                                              ; preds = %349, %341, %320, %289, %261, %222, %194, %158, %135, %117
  %151 = landingpad { i8*, i32 }
          cleanup
  %152 = extractvalue { i8*, i32 } %151, 0
  store i8* %152, i8** %14, align 8
  %153 = extractvalue { i8*, i32 } %151, 1
  store i32 %153, i32* %15, align 4
  br label %393

154:                                              ; preds = %137
  %155 = landingpad { i8*, i32 }
          cleanup
  %156 = extractvalue { i8*, i32 } %155, 0
  store i8* %156, i8** %14, align 8
  %157 = extractvalue { i8*, i32 } %155, 1
  store i32 %157, i32* %15, align 4
  call void @_ZdlPv(i8* %136) #15
  br label %393

158:                                              ; preds = %143
  %159 = load %Array*, %Array** %6, align 8
  %160 = getelementptr inbounds %Array, %Array* %159, i32 0, i32 4
  %161 = load i32, i32* %7, align 4
  %162 = trunc i32 %161 to i8
  %163 = invoke i32 @_ZL8RunCountRKNSt3__16vectorIjNS_9allocatorIjEEEEh(%"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %160, i8 zeroext %162)
          to label %164 unwind label %150

164:                                              ; preds = %158
  store i32 %163, i32* %23, align 4
  %165 = load i32, i32* %23, align 4
  %166 = load %Array*, %Array** %6, align 8
  %167 = getelementptr inbounds %Array, %Array* %166, i32 0, i32 4
  %168 = load i32, i32* %7, align 4
  %169 = sext i32 %168 to i64
  %170 = call nonnull align 4 dereferenceable(4) i32* @_ZNSt3__16vectorIjNS_9allocatorIjEEEixEm(%"class.std::__1::vector"* %167, i64 %169) #11
  %171 = load i32, i32* %170, align 4
  %172 = mul i32 %165, %171
  store i32 %172, i32* %24, align 4
  %173 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %12, i32 0, i32 1
  %174 = load i64, i64* %173, align 8
  %175 = icmp eq i64 %174, 1
  br i1 %175, label %176, label %252

176:                                              ; preds = %164
  %177 = load i32, i32* %23, align 4
  %178 = zext i32 %177 to i64
  %179 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %12, i32 0, i32 2
  %180 = load i64, i64* %179, align 8
  %181 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %12, i32 0, i32 0
  %182 = load i64, i64* %181, align 8
  %183 = sub nsw i64 %180, %182
  %184 = mul nsw i64 %178, %183
  %185 = trunc i64 %184 to i32
  store i32 %185, i32* %25, align 4
  %186 = load i32, i32* %25, align 4
  %187 = zext i32 %186 to i64
  %188 = load i32, i32* %10, align 4
  %189 = zext i32 %188 to i64
  %190 = mul i64 %187, %189
  %191 = call i64 @_ZNSt3__114numeric_limitsImE3maxEv() #11
  %192 = icmp ult i64 %190, %191
  %193 = xor i1 %192, true
  br i1 %193, label %194, label %197

194:                                              ; preds = %176
  invoke void @__assert_rtn(i8* getelementptr inbounds ([25 x i8], [25 x i8]* @__func__.quantum__rt__array_slice, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 586, i8* getelementptr inbounds ([101 x i8], [101 x i8]* @.str.30, i64 0, i64 0)) #14
          to label %195 unwind label %150

195:                                              ; preds = %194
  unreachable

196:                                              ; No predecessors!
  br label %198

197:                                              ; preds = %176
  br label %198

198:                                              ; preds = %197, %196
  %199 = load i32, i32* %25, align 4
  %200 = load i32, i32* %10, align 4
  %201 = mul i32 %199, %200
  %202 = zext i32 %201 to i64
  store i64 %202, i64* %26, align 8
  store i32 0, i32* %27, align 4
  %203 = load i32, i32* %23, align 4
  %204 = zext i32 %203 to i64
  %205 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %12, i32 0, i32 0
  %206 = load i64, i64* %205, align 8
  %207 = mul nsw i64 %204, %206
  %208 = trunc i64 %207 to i32
  store i32 %208, i32* %28, align 4
  br label %209

209:                                              ; preds = %226, %198
  %210 = load i32, i32* %28, align 4
  %211 = load %Array*, %Array** %6, align 8
  %212 = getelementptr inbounds %Array, %Array* %211, i32 0, i32 0
  %213 = load i32, i32* %212, align 8
  %214 = icmp ult i32 %210, %213
  br i1 %214, label %215, label %250

215:                                              ; preds = %209
  %216 = load i32, i32* %27, align 4
  %217 = load %Array*, %Array** %21, align 8
  %218 = getelementptr inbounds %Array, %Array* %217, i32 0, i32 0
  %219 = load i32, i32* %218, align 8
  %220 = icmp ult i32 %216, %219
  %221 = xor i1 %220, true
  br i1 %221, label %222, label %225

222:                                              ; preds = %215
  invoke void @__assert_rtn(i8* getelementptr inbounds ([25 x i8], [25 x i8]* @__func__.quantum__rt__array_slice, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 594, i8* getelementptr inbounds ([19 x i8], [19 x i8]* @.str.31, i64 0, i64 0)) #14
          to label %223 unwind label %150

223:                                              ; preds = %222
  unreachable

224:                                              ; No predecessors!
  br label %226

225:                                              ; preds = %215
  br label %226

226:                                              ; preds = %225, %224
  %227 = load %Array*, %Array** %21, align 8
  %228 = getelementptr inbounds %Array, %Array* %227, i32 0, i32 5
  %229 = load i8*, i8** %228, align 8
  %230 = load i32, i32* %27, align 4
  %231 = load i32, i32* %10, align 4
  %232 = mul i32 %230, %231
  %233 = zext i32 %232 to i64
  %234 = getelementptr inbounds i8, i8* %229, i64 %233
  %235 = load %Array*, %Array** %6, align 8
  %236 = getelementptr inbounds %Array, %Array* %235, i32 0, i32 5
  %237 = load i8*, i8** %236, align 8
  %238 = load i32, i32* %28, align 4
  %239 = load i32, i32* %10, align 4
  %240 = mul i32 %238, %239
  %241 = zext i32 %240 to i64
  %242 = getelementptr inbounds i8, i8* %237, i64 %241
  %243 = load i64, i64* %26, align 8
  call void @llvm.memcpy.p0i8.p0i8.i64(i8* align 1 %234, i8* align 1 %242, i64 %243, i1 false)
  %244 = load i32, i32* %24, align 4
  %245 = load i32, i32* %28, align 4
  %246 = add i32 %245, %244
  store i32 %246, i32* %28, align 4
  %247 = load i32, i32* %25, align 4
  %248 = load i32, i32* %27, align 4
  %249 = add i32 %248, %247
  store i32 %249, i32* %27, align 4
  br label %209

250:                                              ; preds = %209
  %251 = load %Array*, %Array** %21, align 8
  store %Array* %251, %Array** %5, align 8
  store i32 1, i32* %22, align 4
  br label %392

252:                                              ; preds = %164
  %253 = load i32, i32* %23, align 4
  %254 = zext i32 %253 to i64
  %255 = load i32, i32* %10, align 4
  %256 = zext i32 %255 to i64
  %257 = mul i64 %254, %256
  %258 = call i64 @_ZNSt3__114numeric_limitsImE3maxEv() #11
  %259 = icmp ult i64 %257, %258
  %260 = xor i1 %259, true
  br i1 %260, label %261, label %264

261:                                              ; preds = %252
  invoke void @__assert_rtn(i8* getelementptr inbounds ([25 x i8], [25 x i8]* @__func__.quantum__rt__array_slice, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 604, i8* getelementptr inbounds ([107 x i8], [107 x i8]* @.str.32, i64 0, i64 0)) #14
          to label %262 unwind label %150

262:                                              ; preds = %261
  unreachable

263:                                              ; No predecessors!
  br label %265

264:                                              ; preds = %252
  br label %265

265:                                              ; preds = %264, %263
  %266 = load i32, i32* %23, align 4
  %267 = load i32, i32* %10, align 4
  %268 = mul i32 %266, %267
  %269 = zext i32 %268 to i64
  store i64 %269, i64* %29, align 8
  store i32 0, i32* %30, align 4
  %270 = load i32, i32* %23, align 4
  %271 = zext i32 %270 to i64
  %272 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %12, i32 0, i32 0
  %273 = load i64, i64* %272, align 8
  %274 = mul nsw i64 %271, %273
  %275 = trunc i64 %274 to i32
  store i32 %275, i32* %31, align 4
  br label %276

276:                                              ; preds = %386, %265
  %277 = load i32, i32* %31, align 4
  %278 = load %Array*, %Array** %6, align 8
  %279 = getelementptr inbounds %Array, %Array* %278, i32 0, i32 0
  %280 = load i32, i32* %279, align 8
  %281 = icmp ult i32 %277, %280
  br i1 %281, label %282, label %390

282:                                              ; preds = %276
  %283 = load i32, i32* %30, align 4
  %284 = load %Array*, %Array** %21, align 8
  %285 = getelementptr inbounds %Array, %Array* %284, i32 0, i32 0
  %286 = load i32, i32* %285, align 8
  %287 = icmp ult i32 %283, %286
  %288 = xor i1 %287, true
  br i1 %288, label %289, label %292

289:                                              ; preds = %282
  invoke void @__assert_rtn(i8* getelementptr inbounds ([25 x i8], [25 x i8]* @__func__.quantum__rt__array_slice, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 611, i8* getelementptr inbounds ([19 x i8], [19 x i8]* @.str.31, i64 0, i64 0)) #14
          to label %290 unwind label %150

290:                                              ; preds = %289
  unreachable

291:                                              ; No predecessors!
  br label %293

292:                                              ; preds = %282
  br label %293

293:                                              ; preds = %292, %291
  %294 = load i32, i32* %31, align 4
  %295 = zext i32 %294 to i64
  store i64 %295, i64* %32, align 8
  %296 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %12, i32 0, i32 0
  %297 = load i64, i64* %296, align 8
  store i64 %297, i64* %33, align 8
  br label %298

298:                                              ; preds = %381, %293
  %299 = load i64, i64* %33, align 8
  %300 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %12, i32 0, i32 2
  %301 = load i64, i64* %300, align 8
  %302 = icmp ne i64 %299, %301
  br i1 %302, label %303, label %386

303:                                              ; preds = %298
  %304 = load i32, i32* %30, align 4
  %305 = load i32, i32* %10, align 4
  %306 = mul i32 %304, %305
  %307 = zext i32 %306 to i64
  %308 = load i64, i64* %29, align 8
  %309 = add i64 %307, %308
  %310 = load %Array*, %Array** %21, align 8
  %311 = getelementptr inbounds %Array, %Array* %310, i32 0, i32 0
  %312 = load i32, i32* %311, align 8
  %313 = load %Array*, %Array** %21, align 8
  %314 = getelementptr inbounds %Array, %Array* %313, i32 0, i32 1
  %315 = load i32, i32* %314, align 4
  %316 = mul i32 %312, %315
  %317 = zext i32 %316 to i64
  %318 = icmp ule i64 %309, %317
  %319 = xor i1 %318, true
  br i1 %319, label %320, label %323

320:                                              ; preds = %303
  invoke void @__assert_rtn(i8* getelementptr inbounds ([25 x i8], [25 x i8]* @__func__.quantum__rt__array_slice, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 616, i8* getelementptr inbounds ([79 x i8], [79 x i8]* @.str.33, i64 0, i64 0)) #14
          to label %321 unwind label %150

321:                                              ; preds = %320
  unreachable

322:                                              ; No predecessors!
  br label %324

323:                                              ; preds = %303
  br label %324

324:                                              ; preds = %323, %322
  %325 = load i64, i64* %32, align 8
  %326 = load i32, i32* %10, align 4
  %327 = zext i32 %326 to i64
  %328 = mul nsw i64 %325, %327
  %329 = load i64, i64* %29, align 8
  %330 = add nsw i64 %328, %329
  %331 = load %Array*, %Array** %6, align 8
  %332 = getelementptr inbounds %Array, %Array* %331, i32 0, i32 0
  %333 = load i32, i32* %332, align 8
  %334 = load %Array*, %Array** %6, align 8
  %335 = getelementptr inbounds %Array, %Array* %334, i32 0, i32 1
  %336 = load i32, i32* %335, align 4
  %337 = mul i32 %333, %336
  %338 = zext i32 %337 to i64
  %339 = icmp sle i64 %330, %338
  %340 = xor i1 %339, true
  br i1 %340, label %341, label %344

341:                                              ; preds = %324
  invoke void @__assert_rtn(i8* getelementptr inbounds ([25 x i8], [25 x i8]* @__func__.quantum__rt__array_slice, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 618, i8* getelementptr inbounds ([102 x i8], [102 x i8]* @.str.34, i64 0, i64 0)) #14
          to label %342 unwind label %150

342:                                              ; preds = %341
  unreachable

343:                                              ; No predecessors!
  br label %345

344:                                              ; preds = %324
  br label %345

345:                                              ; preds = %344, %343
  %346 = load i64, i64* %32, align 8
  %347 = icmp sge i64 %346, 0
  %348 = xor i1 %347, true
  br i1 %348, label %349, label %352

349:                                              ; preds = %345
  invoke void @__assert_rtn(i8* getelementptr inbounds ([25 x i8], [25 x i8]* @__func__.quantum__rt__array_slice, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 619, i8* getelementptr inbounds ([14 x i8], [14 x i8]* @.str.35, i64 0, i64 0)) #14
          to label %350 unwind label %150

350:                                              ; preds = %349
  unreachable

351:                                              ; No predecessors!
  br label %353

352:                                              ; preds = %345
  br label %353

353:                                              ; preds = %352, %351
  %354 = load %Array*, %Array** %21, align 8
  %355 = getelementptr inbounds %Array, %Array* %354, i32 0, i32 5
  %356 = load i8*, i8** %355, align 8
  %357 = load i32, i32* %30, align 4
  %358 = load i32, i32* %10, align 4
  %359 = mul i32 %357, %358
  %360 = zext i32 %359 to i64
  %361 = getelementptr inbounds i8, i8* %356, i64 %360
  %362 = load %Array*, %Array** %6, align 8
  %363 = getelementptr inbounds %Array, %Array* %362, i32 0, i32 5
  %364 = load i8*, i8** %363, align 8
  %365 = load i64, i64* %32, align 8
  %366 = load i32, i32* %10, align 4
  %367 = zext i32 %366 to i64
  %368 = mul nsw i64 %365, %367
  %369 = getelementptr inbounds i8, i8* %364, i64 %368
  %370 = load i64, i64* %29, align 8
  call void @llvm.memcpy.p0i8.p0i8.i64(i8* align 1 %361, i8* align 1 %369, i64 %370, i1 false)
  %371 = load i32, i32* %23, align 4
  %372 = zext i32 %371 to i64
  %373 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %12, i32 0, i32 1
  %374 = load i64, i64* %373, align 8
  %375 = mul nsw i64 %372, %374
  %376 = load i64, i64* %32, align 8
  %377 = add nsw i64 %376, %375
  store i64 %377, i64* %32, align 8
  %378 = load i32, i32* %23, align 4
  %379 = load i32, i32* %30, align 4
  %380 = add i32 %379, %378
  store i32 %380, i32* %30, align 4
  br label %381

381:                                              ; preds = %353
  %382 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %12, i32 0, i32 1
  %383 = load i64, i64* %382, align 8
  %384 = load i64, i64* %33, align 8
  %385 = add nsw i64 %384, %383
  store i64 %385, i64* %33, align 8
  br label %298

386:                                              ; preds = %298
  %387 = load i32, i32* %24, align 4
  %388 = load i32, i32* %31, align 4
  %389 = add i32 %388, %387
  store i32 %389, i32* %31, align 4
  br label %276

390:                                              ; preds = %276
  %391 = load %Array*, %Array** %21, align 8
  store %Array* %391, %Array** %5, align 8
  store i32 1, i32* %22, align 4
  br label %392

392:                                              ; preds = %390, %250, %148
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEED1Ev(%"class.std::__1::vector"* %16) #11
  br label %394

393:                                              ; preds = %154, %150
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEED1Ev(%"class.std::__1::vector"* %16) #11
  br label %396

394:                                              ; preds = %392, %114, %85
  %395 = load %Array*, %Array** %5, align 8
  ret %Array* %395

396:                                              ; preds = %393, %94
  %397 = load i8*, i8** %14, align 8
  %398 = load i32, i32* %15, align 4
  %399 = insertvalue { i8*, i32 } undef, i8* %397, 0
  %400 = insertvalue { i8*, i32 } %399, i32 %398, 1
  resume { i8*, i32 } %400
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZN12CheckedRangeC1ERK8QirRangex(%struct.CheckedRange* %0, %struct.QirRange* nonnull align 8 dereferenceable(24) %1, i64 %2) unnamed_addr #0 align 2 {
  %4 = alloca %struct.CheckedRange*, align 8
  %5 = alloca %struct.QirRange*, align 8
  %6 = alloca i64, align 8
  store %struct.CheckedRange* %0, %struct.CheckedRange** %4, align 8
  store %struct.QirRange* %1, %struct.QirRange** %5, align 8
  store i64 %2, i64* %6, align 8
  %7 = load %struct.CheckedRange*, %struct.CheckedRange** %4, align 8
  %8 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %9 = load i64, i64* %6, align 8
  call void @_ZN12CheckedRangeC2ERK8QirRangex(%struct.CheckedRange* %7, %struct.QirRange* nonnull align 8 dereferenceable(24) %8, i64 %9)
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr zeroext i1 @_ZNK12CheckedRange7IsEmptyEv(%struct.CheckedRange* %0) #1 align 2 {
  %2 = alloca %struct.CheckedRange*, align 8
  store %struct.CheckedRange* %0, %struct.CheckedRange** %2, align 8
  %3 = load %struct.CheckedRange*, %struct.CheckedRange** %2, align 8
  %4 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %3, i32 0, i32 3
  %5 = load i64, i64* %4, align 8
  %6 = icmp eq i64 %5, 0
  ret i1 %6
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i32 @_ZNSt3__110accumulateINS_11__wrap_iterIPjEEjNS_10multipliesIjEEEET0_T_S7_S6_T1_(i32* %0, i32* %1, i32 %2) #1 {
  %4 = alloca %"class.std::__1::__wrap_iter", align 8
  %5 = alloca %"class.std::__1::__wrap_iter", align 8
  %6 = alloca %"struct.std::__1::multiplies", align 1
  %7 = alloca i32, align 4
  %8 = getelementptr inbounds %"class.std::__1::__wrap_iter", %"class.std::__1::__wrap_iter"* %4, i32 0, i32 0
  store i32* %0, i32** %8, align 8
  %9 = getelementptr inbounds %"class.std::__1::__wrap_iter", %"class.std::__1::__wrap_iter"* %5, i32 0, i32 0
  store i32* %1, i32** %9, align 8
  store i32 %2, i32* %7, align 4
  br label %10

10:                                               ; preds = %15, %3
  %11 = call zeroext i1 @_ZNSt3__1neIPjEEbRKNS_11__wrap_iterIT_EES6_(%"class.std::__1::__wrap_iter"* nonnull align 8 dereferenceable(8) %4, %"class.std::__1::__wrap_iter"* nonnull align 8 dereferenceable(8) %5) #11
  br i1 %11, label %12, label %17

12:                                               ; preds = %10
  %13 = call nonnull align 4 dereferenceable(4) i32* @_ZNKSt3__111__wrap_iterIPjEdeEv(%"class.std::__1::__wrap_iter"* %4) #11
  %14 = call i32 @_ZNKSt3__110multipliesIjEclERKjS3_(%"struct.std::__1::multiplies"* %6, i32* nonnull align 4 dereferenceable(4) %7, i32* nonnull align 4 dereferenceable(4) %13)
  store i32 %14, i32* %7, align 4
  br label %15

15:                                               ; preds = %12
  %16 = call nonnull align 8 dereferenceable(8) %"class.std::__1::__wrap_iter"* @_ZNSt3__111__wrap_iterIPjEppEv(%"class.std::__1::__wrap_iter"* %4) #11
  br label %10

17:                                               ; preds = %10
  %18 = load i32, i32* %7, align 4
  ret i32 %18
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i32* @_ZNSt3__16vectorIjNS_9allocatorIjEEE5beginEv(%"class.std::__1::vector"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::__wrap_iter", align 8
  %3 = alloca %"class.std::__1::vector"*, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %3, align 8
  %4 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  %5 = bitcast %"class.std::__1::vector"* %4 to %"class.std::__1::__vector_base"*
  %6 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %5, i32 0, i32 0
  %7 = load i32*, i32** %6, align 8
  %8 = call i32* @_ZNSt3__16vectorIjNS_9allocatorIjEEE11__make_iterEPj(%"class.std::__1::vector"* %4, i32* %7) #11
  %9 = getelementptr inbounds %"class.std::__1::__wrap_iter", %"class.std::__1::__wrap_iter"* %2, i32 0, i32 0
  store i32* %8, i32** %9, align 8
  %10 = getelementptr inbounds %"class.std::__1::__wrap_iter", %"class.std::__1::__wrap_iter"* %2, i32 0, i32 0
  %11 = load i32*, i32** %10, align 8
  ret i32* %11
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i32* @_ZNSt3__16vectorIjNS_9allocatorIjEEE3endEv(%"class.std::__1::vector"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::__wrap_iter", align 8
  %3 = alloca %"class.std::__1::vector"*, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %3, align 8
  %4 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  %5 = bitcast %"class.std::__1::vector"* %4 to %"class.std::__1::__vector_base"*
  %6 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %5, i32 0, i32 1
  %7 = load i32*, i32** %6, align 8
  %8 = call i32* @_ZNSt3__16vectorIjNS_9allocatorIjEEE11__make_iterEPj(%"class.std::__1::vector"* %4, i32* %7) #11
  %9 = getelementptr inbounds %"class.std::__1::__wrap_iter", %"class.std::__1::__wrap_iter"* %2, i32 0, i32 0
  store i32* %8, i32** %9, align 8
  %10 = getelementptr inbounds %"class.std::__1::__wrap_iter", %"class.std::__1::__wrap_iter"* %2, i32 0, i32 0
  %11 = load i32*, i32** %10, align 8
  ret i32* %11
}

; Function Attrs: noinline optnone ssp uwtable
define internal i32 @_ZL8RunCountRKNSt3__16vectorIjNS_9allocatorIjEEEEh(%"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %0, i8 zeroext %1) #0 {
  %3 = alloca %"class.std::__1::vector"*, align 8
  %4 = alloca i8, align 1
  %5 = alloca %"class.std::__1::__wrap_iter.16", align 8
  %6 = alloca %"class.std::__1::__wrap_iter.16", align 8
  %7 = alloca %"class.std::__1::__wrap_iter.16", align 8
  %8 = alloca %"class.std::__1::__wrap_iter.16", align 8
  %9 = alloca %"struct.std::__1::multiplies", align 1
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %3, align 8
  store i8 %1, i8* %4, align 1
  %10 = load i8, i8* %4, align 1
  %11 = zext i8 %10 to i32
  %12 = icmp sle i32 0, %11
  br i1 %12, label %13, label %19

13:                                               ; preds = %2
  %14 = load i8, i8* %4, align 1
  %15 = zext i8 %14 to i64
  %16 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  %17 = call i64 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE4sizeEv(%"class.std::__1::vector"* %16) #11
  %18 = icmp ult i64 %15, %17
  br label %19

19:                                               ; preds = %13, %2
  %20 = phi i1 [ false, %2 ], [ %18, %13 ]
  %21 = xor i1 %20, true
  br i1 %21, label %22, label %24

22:                                               ; preds = %19
  call void @__assert_rtn(i8* getelementptr inbounds ([9 x i8], [9 x i8]* @__func__._ZL8RunCountRKNSt3__16vectorIjNS_9allocatorIjEEEEh, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 235, i8* getelementptr inbounds ([64 x i8], [64 x i8]* @.str.42, i64 0, i64 0)) #14
  unreachable

23:                                               ; No predecessors!
  br label %25

24:                                               ; preds = %19
  br label %25

25:                                               ; preds = %24, %23
  %26 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  %27 = call i32* @_ZNKSt3__16vectorIjNS_9allocatorIjEEE5beginEv(%"class.std::__1::vector"* %26) #11
  %28 = getelementptr inbounds %"class.std::__1::__wrap_iter.16", %"class.std::__1::__wrap_iter.16"* %7, i32 0, i32 0
  store i32* %27, i32** %28, align 8
  %29 = load i8, i8* %4, align 1
  %30 = zext i8 %29 to i64
  %31 = call i32* @_ZNKSt3__111__wrap_iterIPKjEplEl(%"class.std::__1::__wrap_iter.16"* %7, i64 %30) #11
  %32 = getelementptr inbounds %"class.std::__1::__wrap_iter.16", %"class.std::__1::__wrap_iter.16"* %6, i32 0, i32 0
  store i32* %31, i32** %32, align 8
  %33 = call i32* @_ZNKSt3__111__wrap_iterIPKjEplEl(%"class.std::__1::__wrap_iter.16"* %6, i64 1) #11
  %34 = getelementptr inbounds %"class.std::__1::__wrap_iter.16", %"class.std::__1::__wrap_iter.16"* %5, i32 0, i32 0
  store i32* %33, i32** %34, align 8
  %35 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  %36 = call i32* @_ZNKSt3__16vectorIjNS_9allocatorIjEEE3endEv(%"class.std::__1::vector"* %35) #11
  %37 = getelementptr inbounds %"class.std::__1::__wrap_iter.16", %"class.std::__1::__wrap_iter.16"* %8, i32 0, i32 0
  store i32* %36, i32** %37, align 8
  %38 = getelementptr inbounds %"class.std::__1::__wrap_iter.16", %"class.std::__1::__wrap_iter.16"* %5, i32 0, i32 0
  %39 = load i32*, i32** %38, align 8
  %40 = getelementptr inbounds %"class.std::__1::__wrap_iter.16", %"class.std::__1::__wrap_iter.16"* %8, i32 0, i32 0
  %41 = load i32*, i32** %40, align 8
  %42 = call zeroext i8 @_ZNSt3__110accumulateINS_11__wrap_iterIPKjEEhNS_10multipliesIjEEEET0_T_S8_S7_T1_(i32* %39, i32* %41, i8 zeroext 1)
  %43 = zext i8 %42 to i32
  ret i32 %43
}

; Function Attrs: noinline optnone ssp uwtable
define %Array* @__quantum__rt__array_project(%Array* %0, i32 %1, i64 %2) #0 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %4 = alloca %Array*, align 8
  %5 = alloca %Array*, align 8
  %6 = alloca i32, align 4
  %7 = alloca i64, align 8
  %8 = alloca i32, align 4
  %9 = alloca i8, align 1
  %10 = alloca %"class.std::__1::vector", align 8
  %11 = alloca %"class.std::__1::__wrap_iter.16", align 8
  %12 = alloca %"class.std::__1::__wrap_iter", align 8
  %13 = alloca %"class.std::__1::__wrap_iter", align 8
  %14 = alloca i8*, align 8
  %15 = alloca i32, align 4
  %16 = alloca %"class.std::__1::__wrap_iter", align 8
  %17 = alloca i32, align 4
  %18 = alloca %"class.std::__1::__wrap_iter", align 8
  %19 = alloca %"class.std::__1::__wrap_iter", align 8
  %20 = alloca %"struct.std::__1::multiplies", align 1
  %21 = alloca %Array*, align 8
  %22 = alloca i32, align 4
  %23 = alloca i32, align 4
  %24 = alloca i32, align 4
  %25 = alloca i64, align 8
  %26 = alloca i32, align 4
  %27 = alloca i32, align 4
  store %Array* %0, %Array** %5, align 8
  store i32 %1, i32* %6, align 4
  store i64 %2, i64* %7, align 8
  %28 = load %Array*, %Array** %5, align 8
  %29 = icmp ne %Array* %28, null
  %30 = xor i1 %29, true
  br i1 %30, label %31, label %33

31:                                               ; preds = %3
  call void @__assert_rtn(i8* getelementptr inbounds ([29 x i8], [29 x i8]* @__func__.__quantum__rt__array_project, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 636, i8* getelementptr inbounds ([17 x i8], [17 x i8]* @.str.23, i64 0, i64 0)) #14
  unreachable

32:                                               ; No predecessors!
  br label %34

33:                                               ; preds = %3
  br label %34

34:                                               ; preds = %33, %32
  %35 = load i32, i32* %6, align 4
  %36 = icmp sge i32 %35, 0
  br i1 %36, label %37, label %44

37:                                               ; preds = %34
  %38 = load i32, i32* %6, align 4
  %39 = load %Array*, %Array** %5, align 8
  %40 = getelementptr inbounds %Array, %Array* %39, i32 0, i32 2
  %41 = load i8, i8* %40, align 8
  %42 = zext i8 %41 to i32
  %43 = icmp slt i32 %38, %42
  br label %44

44:                                               ; preds = %37, %34
  %45 = phi i1 [ false, %34 ], [ %43, %37 ]
  %46 = xor i1 %45, true
  br i1 %46, label %47, label %49

47:                                               ; preds = %44
  call void @__assert_rtn(i8* getelementptr inbounds ([29 x i8], [29 x i8]* @__func__.__quantum__rt__array_project, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 637, i8* getelementptr inbounds ([36 x i8], [36 x i8]* @.str.29, i64 0, i64 0)) #14
  unreachable

48:                                               ; No predecessors!
  br label %50

49:                                               ; preds = %44
  br label %50

50:                                               ; preds = %49, %48
  %51 = load %Array*, %Array** %5, align 8
  %52 = getelementptr inbounds %Array, %Array* %51, i32 0, i32 2
  %53 = load i8, i8* %52, align 8
  %54 = zext i8 %53 to i32
  %55 = icmp sgt i32 %54, 1
  %56 = xor i1 %55, true
  br i1 %56, label %57, label %59

57:                                               ; preds = %50
  call void @__assert_rtn(i8* getelementptr inbounds ([29 x i8], [29 x i8]* @__func__.__quantum__rt__array_project, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 638, i8* getelementptr inbounds ([22 x i8], [22 x i8]* @.str.36, i64 0, i64 0)) #14
  unreachable

58:                                               ; No predecessors!
  br label %60

59:                                               ; preds = %50
  br label %60

60:                                               ; preds = %59, %58
  %61 = load i64, i64* %7, align 8
  %62 = icmp sge i64 %61, 0
  br i1 %62, label %63, label %73

63:                                               ; preds = %60
  %64 = load i64, i64* %7, align 8
  %65 = load %Array*, %Array** %5, align 8
  %66 = getelementptr inbounds %Array, %Array* %65, i32 0, i32 4
  %67 = load i32, i32* %6, align 4
  %68 = sext i32 %67 to i64
  %69 = call nonnull align 4 dereferenceable(4) i32* @_ZNSt3__16vectorIjNS_9allocatorIjEEEixEm(%"class.std::__1::vector"* %66, i64 %68) #11
  %70 = load i32, i32* %69, align 4
  %71 = zext i32 %70 to i64
  %72 = icmp slt i64 %64, %71
  br label %73

73:                                               ; preds = %63, %60
  %74 = phi i1 [ false, %60 ], [ %72, %63 ]
  %75 = xor i1 %74, true
  br i1 %75, label %76, label %78

76:                                               ; preds = %73
  call void @__assert_rtn(i8* getelementptr inbounds ([29 x i8], [29 x i8]* @__func__.__quantum__rt__array_project, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 639, i8* getelementptr inbounds ([57 x i8], [57 x i8]* @.str.37, i64 0, i64 0)) #14
  unreachable

77:                                               ; No predecessors!
  br label %79

78:                                               ; preds = %73
  br label %79

79:                                               ; preds = %78, %77
  %80 = load %Array*, %Array** %5, align 8
  %81 = getelementptr inbounds %Array, %Array* %80, i32 0, i32 1
  %82 = load i32, i32* %81, align 4
  store i32 %82, i32* %8, align 4
  %83 = load %Array*, %Array** %5, align 8
  %84 = getelementptr inbounds %Array, %Array* %83, i32 0, i32 2
  %85 = load i8, i8* %84, align 8
  store i8 %85, i8* %9, align 1
  %86 = load %Array*, %Array** %5, align 8
  %87 = getelementptr inbounds %Array, %Array* %86, i32 0, i32 4
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEEC1ERKS3_(%"class.std::__1::vector"* %10, %"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %87)
  %88 = call i32* @_ZNSt3__16vectorIjNS_9allocatorIjEEE5beginEv(%"class.std::__1::vector"* %10) #11
  %89 = getelementptr inbounds %"class.std::__1::__wrap_iter", %"class.std::__1::__wrap_iter"* %13, i32 0, i32 0
  store i32* %88, i32** %89, align 8
  %90 = load i32, i32* %6, align 4
  %91 = sext i32 %90 to i64
  %92 = call i32* @_ZNKSt3__111__wrap_iterIPjEplEl(%"class.std::__1::__wrap_iter"* %13, i64 %91) #11
  %93 = getelementptr inbounds %"class.std::__1::__wrap_iter", %"class.std::__1::__wrap_iter"* %12, i32 0, i32 0
  store i32* %92, i32** %93, align 8
  call void @_ZNSt3__111__wrap_iterIPKjEC1IPjEERKNS0_IT_EEPNS_9enable_ifIXsr14is_convertibleIS6_S2_EE5valueEvE4typeE(%"class.std::__1::__wrap_iter.16"* %11, %"class.std::__1::__wrap_iter"* nonnull align 8 dereferenceable(8) %12, i8* null) #11
  %94 = getelementptr inbounds %"class.std::__1::__wrap_iter.16", %"class.std::__1::__wrap_iter.16"* %11, i32 0, i32 0
  %95 = load i32*, i32** %94, align 8
  %96 = invoke i32* @_ZNSt3__16vectorIjNS_9allocatorIjEEE5eraseENS_11__wrap_iterIPKjEE(%"class.std::__1::vector"* %10, i32* %95)
          to label %97 unwind label %126

97:                                               ; preds = %79
  %98 = getelementptr inbounds %"class.std::__1::__wrap_iter", %"class.std::__1::__wrap_iter"* %16, i32 0, i32 0
  store i32* %96, i32** %98, align 8
  %99 = call i32* @_ZNSt3__16vectorIjNS_9allocatorIjEEE5beginEv(%"class.std::__1::vector"* %10) #11
  %100 = getelementptr inbounds %"class.std::__1::__wrap_iter", %"class.std::__1::__wrap_iter"* %18, i32 0, i32 0
  store i32* %99, i32** %100, align 8
  %101 = call i32* @_ZNSt3__16vectorIjNS_9allocatorIjEEE3endEv(%"class.std::__1::vector"* %10) #11
  %102 = getelementptr inbounds %"class.std::__1::__wrap_iter", %"class.std::__1::__wrap_iter"* %19, i32 0, i32 0
  store i32* %101, i32** %102, align 8
  %103 = getelementptr inbounds %"class.std::__1::__wrap_iter", %"class.std::__1::__wrap_iter"* %18, i32 0, i32 0
  %104 = load i32*, i32** %103, align 8
  %105 = getelementptr inbounds %"class.std::__1::__wrap_iter", %"class.std::__1::__wrap_iter"* %19, i32 0, i32 0
  %106 = load i32*, i32** %105, align 8
  %107 = invoke i32 @_ZNSt3__110accumulateINS_11__wrap_iterIPjEEjNS_10multipliesIjEEEET0_T_S7_S6_T1_(i32* %104, i32* %106, i32 1)
          to label %108 unwind label %126

108:                                              ; preds = %97
  store i32 %107, i32* %17, align 4
  %109 = invoke noalias nonnull i8* @_Znwm(i64 64) #16
          to label %110 unwind label %126

110:                                              ; preds = %108
  %111 = bitcast i8* %109 to %Array*
  %112 = load i32, i32* %17, align 4
  %113 = load i32, i32* %8, align 4
  %114 = load i8, i8* %9, align 1
  %115 = zext i8 %114 to i32
  %116 = sub nsw i32 %115, 1
  %117 = trunc i32 %116 to i8
  %118 = call nonnull align 8 dereferenceable(24) %"class.std::__1::vector"* @_ZNSt3__14moveIRNS_6vectorIjNS_9allocatorIjEEEEEEONS_16remove_referenceIT_E4typeEOS7_(%"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %10) #11
  invoke void @_ZN8QirArrayC1EjjhONSt3__16vectorIjNS0_9allocatorIjEEEE(%Array* %111, i32 %112, i32 %113, i8 zeroext %117, %"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %118)
          to label %119 unwind label %130

119:                                              ; preds = %110
  store %Array* %111, %Array** %21, align 8
  %120 = load %Array*, %Array** %21, align 8
  %121 = getelementptr inbounds %Array, %Array* %120, i32 0, i32 5
  %122 = load i8*, i8** %121, align 8
  %123 = icmp eq i8* null, %122
  br i1 %123, label %124, label %134

124:                                              ; preds = %119
  %125 = load %Array*, %Array** %21, align 8
  store %Array* %125, %Array** %4, align 8
  store i32 1, i32* %22, align 4
  br label %214

126:                                              ; preds = %184, %157, %134, %108, %97, %79
  %127 = landingpad { i8*, i32 }
          cleanup
  %128 = extractvalue { i8*, i32 } %127, 0
  store i8* %128, i8** %14, align 8
  %129 = extractvalue { i8*, i32 } %127, 1
  store i32 %129, i32* %15, align 4
  br label %216

130:                                              ; preds = %110
  %131 = landingpad { i8*, i32 }
          cleanup
  %132 = extractvalue { i8*, i32 } %131, 0
  store i8* %132, i8** %14, align 8
  %133 = extractvalue { i8*, i32 } %131, 1
  store i32 %133, i32* %15, align 4
  call void @_ZdlPv(i8* %109) #15
  br label %216

134:                                              ; preds = %119
  %135 = load %Array*, %Array** %5, align 8
  %136 = getelementptr inbounds %Array, %Array* %135, i32 0, i32 4
  %137 = load i32, i32* %6, align 4
  %138 = trunc i32 %137 to i8
  %139 = invoke i32 @_ZL8RunCountRKNSt3__16vectorIjNS_9allocatorIjEEEEh(%"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %136, i8 zeroext %138)
          to label %140 unwind label %126

140:                                              ; preds = %134
  store i32 %139, i32* %23, align 4
  %141 = load i32, i32* %23, align 4
  %142 = load %Array*, %Array** %5, align 8
  %143 = getelementptr inbounds %Array, %Array* %142, i32 0, i32 4
  %144 = load i32, i32* %6, align 4
  %145 = sext i32 %144 to i64
  %146 = call nonnull align 4 dereferenceable(4) i32* @_ZNSt3__16vectorIjNS_9allocatorIjEEEixEm(%"class.std::__1::vector"* %143, i64 %145) #11
  %147 = load i32, i32* %146, align 4
  %148 = mul i32 %141, %147
  store i32 %148, i32* %24, align 4
  %149 = load i32, i32* %23, align 4
  %150 = zext i32 %149 to i64
  %151 = load i32, i32* %8, align 4
  %152 = zext i32 %151 to i64
  %153 = mul i64 %150, %152
  %154 = call i64 @_ZNSt3__114numeric_limitsImE3maxEv() #11
  %155 = icmp ult i64 %153, %154
  %156 = xor i1 %155, true
  br i1 %156, label %157, label %160

157:                                              ; preds = %140
  invoke void @__assert_rtn(i8* getelementptr inbounds ([29 x i8], [29 x i8]* @__func__.__quantum__rt__array_project, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 660, i8* getelementptr inbounds ([107 x i8], [107 x i8]* @.str.32, i64 0, i64 0)) #14
          to label %158 unwind label %126

158:                                              ; preds = %157
  unreachable

159:                                              ; No predecessors!
  br label %161

160:                                              ; preds = %140
  br label %161

161:                                              ; preds = %160, %159
  %162 = load i32, i32* %23, align 4
  %163 = load i32, i32* %8, align 4
  %164 = mul i32 %162, %163
  %165 = zext i32 %164 to i64
  store i64 %165, i64* %25, align 8
  store i32 0, i32* %26, align 4
  %166 = load i32, i32* %23, align 4
  %167 = zext i32 %166 to i64
  %168 = load i64, i64* %7, align 8
  %169 = mul nsw i64 %167, %168
  %170 = trunc i64 %169 to i32
  store i32 %170, i32* %27, align 4
  br label %171

171:                                              ; preds = %188, %161
  %172 = load i32, i32* %27, align 4
  %173 = load %Array*, %Array** %5, align 8
  %174 = getelementptr inbounds %Array, %Array* %173, i32 0, i32 0
  %175 = load i32, i32* %174, align 8
  %176 = icmp ult i32 %172, %175
  br i1 %176, label %177, label %212

177:                                              ; preds = %171
  %178 = load i32, i32* %26, align 4
  %179 = load %Array*, %Array** %21, align 8
  %180 = getelementptr inbounds %Array, %Array* %179, i32 0, i32 0
  %181 = load i32, i32* %180, align 8
  %182 = icmp ult i32 %178, %181
  %183 = xor i1 %182, true
  br i1 %183, label %184, label %187

184:                                              ; preds = %177
  invoke void @__assert_rtn(i8* getelementptr inbounds ([29 x i8], [29 x i8]* @__func__.__quantum__rt__array_project, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 669, i8* getelementptr inbounds ([21 x i8], [21 x i8]* @.str.38, i64 0, i64 0)) #14
          to label %185 unwind label %126

185:                                              ; preds = %184
  unreachable

186:                                              ; No predecessors!
  br label %188

187:                                              ; preds = %177
  br label %188

188:                                              ; preds = %187, %186
  %189 = load %Array*, %Array** %21, align 8
  %190 = getelementptr inbounds %Array, %Array* %189, i32 0, i32 5
  %191 = load i8*, i8** %190, align 8
  %192 = load i32, i32* %26, align 4
  %193 = load i32, i32* %8, align 4
  %194 = mul i32 %192, %193
  %195 = zext i32 %194 to i64
  %196 = getelementptr inbounds i8, i8* %191, i64 %195
  %197 = load %Array*, %Array** %5, align 8
  %198 = getelementptr inbounds %Array, %Array* %197, i32 0, i32 5
  %199 = load i8*, i8** %198, align 8
  %200 = load i32, i32* %27, align 4
  %201 = load i32, i32* %8, align 4
  %202 = mul i32 %200, %201
  %203 = zext i32 %202 to i64
  %204 = getelementptr inbounds i8, i8* %199, i64 %203
  %205 = load i64, i64* %25, align 8
  call void @llvm.memcpy.p0i8.p0i8.i64(i8* align 1 %196, i8* align 1 %204, i64 %205, i1 false)
  %206 = load i32, i32* %24, align 4
  %207 = load i32, i32* %27, align 4
  %208 = add i32 %207, %206
  store i32 %208, i32* %27, align 4
  %209 = load i32, i32* %23, align 4
  %210 = load i32, i32* %26, align 4
  %211 = add i32 %210, %209
  store i32 %211, i32* %26, align 4
  br label %171

212:                                              ; preds = %171
  %213 = load %Array*, %Array** %21, align 8
  store %Array* %213, %Array** %4, align 8
  store i32 1, i32* %22, align 4
  br label %214

214:                                              ; preds = %212, %124
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEED1Ev(%"class.std::__1::vector"* %10) #11
  %215 = load %Array*, %Array** %4, align 8
  ret %Array* %215

216:                                              ; preds = %130, %126
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEED1Ev(%"class.std::__1::vector"* %10) #11
  br label %217

217:                                              ; preds = %216
  %218 = load i8*, i8** %14, align 8
  %219 = load i32, i32* %15, align 4
  %220 = insertvalue { i8*, i32 } undef, i8* %218, 0
  %221 = insertvalue { i8*, i32 } %220, i32 %219, 1
  resume { i8*, i32 } %221
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr hidden i32* @_ZNSt3__16vectorIjNS_9allocatorIjEEE5eraseENS_11__wrap_iterIPKjEE(%"class.std::__1::vector"* %0, i32* %1) #0 align 2 {
  %3 = alloca %"class.std::__1::__wrap_iter", align 8
  %4 = alloca %"class.std::__1::__wrap_iter.16", align 8
  %5 = alloca %"class.std::__1::vector"*, align 8
  %6 = alloca i64, align 8
  %7 = alloca %"class.std::__1::__wrap_iter.16", align 8
  %8 = alloca i32*, align 8
  %9 = getelementptr inbounds %"class.std::__1::__wrap_iter.16", %"class.std::__1::__wrap_iter.16"* %4, i32 0, i32 0
  store i32* %1, i32** %9, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %5, align 8
  %10 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %5, align 8
  %11 = call i32* @_ZNKSt3__16vectorIjNS_9allocatorIjEEE6cbeginEv(%"class.std::__1::vector"* %10) #11
  %12 = getelementptr inbounds %"class.std::__1::__wrap_iter.16", %"class.std::__1::__wrap_iter.16"* %7, i32 0, i32 0
  store i32* %11, i32** %12, align 8
  %13 = call i64 @_ZNSt3__1miIPKjS2_EEDTmicldtfp_4baseEcldtfp0_4baseEERKNS_11__wrap_iterIT_EERKNS4_IT0_EE(%"class.std::__1::__wrap_iter.16"* nonnull align 8 dereferenceable(8) %4, %"class.std::__1::__wrap_iter.16"* nonnull align 8 dereferenceable(8) %7) #11
  store i64 %13, i64* %6, align 8
  %14 = bitcast %"class.std::__1::vector"* %10 to %"class.std::__1::__vector_base"*
  %15 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %14, i32 0, i32 0
  %16 = load i32*, i32** %15, align 8
  %17 = load i64, i64* %6, align 8
  %18 = getelementptr inbounds i32, i32* %16, i64 %17
  store i32* %18, i32** %8, align 8
  %19 = load i32*, i32** %8, align 8
  %20 = getelementptr inbounds i32, i32* %19, i64 1
  %21 = bitcast %"class.std::__1::vector"* %10 to %"class.std::__1::__vector_base"*
  %22 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %21, i32 0, i32 1
  %23 = load i32*, i32** %22, align 8
  %24 = load i32*, i32** %8, align 8
  %25 = call i32* @_ZNSt3__14moveIPjS1_EET0_T_S3_S2_(i32* %20, i32* %23, i32* %24)
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEE17__destruct_at_endEPj(%"class.std::__1::vector"* %10, i32* %25) #11
  %26 = load i32*, i32** %8, align 8
  %27 = getelementptr inbounds i32, i32* %26, i64 -1
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEE27__invalidate_iterators_pastEPj(%"class.std::__1::vector"* %10, i32* %27)
  %28 = load i32*, i32** %8, align 8
  %29 = call i32* @_ZNSt3__16vectorIjNS_9allocatorIjEEE11__make_iterEPj(%"class.std::__1::vector"* %10, i32* %28) #11
  %30 = getelementptr inbounds %"class.std::__1::__wrap_iter", %"class.std::__1::__wrap_iter"* %3, i32 0, i32 0
  store i32* %29, i32** %30, align 8
  %31 = getelementptr inbounds %"class.std::__1::__wrap_iter", %"class.std::__1::__wrap_iter"* %3, i32 0, i32 0
  %32 = load i32*, i32** %31, align 8
  ret i32* %32
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i32* @_ZNKSt3__111__wrap_iterIPjEplEl(%"class.std::__1::__wrap_iter"* %0, i64 %1) #1 align 2 {
  %3 = alloca %"class.std::__1::__wrap_iter", align 8
  %4 = alloca %"class.std::__1::__wrap_iter"*, align 8
  %5 = alloca i64, align 8
  store %"class.std::__1::__wrap_iter"* %0, %"class.std::__1::__wrap_iter"** %4, align 8
  store i64 %1, i64* %5, align 8
  %6 = load %"class.std::__1::__wrap_iter"*, %"class.std::__1::__wrap_iter"** %4, align 8
  %7 = bitcast %"class.std::__1::__wrap_iter"* %3 to i8*
  %8 = bitcast %"class.std::__1::__wrap_iter"* %6 to i8*
  call void @llvm.memcpy.p0i8.p0i8.i64(i8* align 8 %7, i8* align 8 %8, i64 8, i1 false)
  %9 = load i64, i64* %5, align 8
  %10 = call nonnull align 8 dereferenceable(8) %"class.std::__1::__wrap_iter"* @_ZNSt3__111__wrap_iterIPjEpLEl(%"class.std::__1::__wrap_iter"* %3, i64 %9) #11
  %11 = getelementptr inbounds %"class.std::__1::__wrap_iter", %"class.std::__1::__wrap_iter"* %3, i32 0, i32 0
  %12 = load i32*, i32** %11, align 8
  ret i32* %12
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZNSt3__111__wrap_iterIPKjEC1IPjEERKNS0_IT_EEPNS_9enable_ifIXsr14is_convertibleIS6_S2_EE5valueEvE4typeE(%"class.std::__1::__wrap_iter.16"* %0, %"class.std::__1::__wrap_iter"* nonnull align 8 dereferenceable(8) %1, i8* %2) unnamed_addr #1 align 2 {
  %4 = alloca %"class.std::__1::__wrap_iter.16"*, align 8
  %5 = alloca %"class.std::__1::__wrap_iter"*, align 8
  %6 = alloca i8*, align 8
  store %"class.std::__1::__wrap_iter.16"* %0, %"class.std::__1::__wrap_iter.16"** %4, align 8
  store %"class.std::__1::__wrap_iter"* %1, %"class.std::__1::__wrap_iter"** %5, align 8
  store i8* %2, i8** %6, align 8
  %7 = load %"class.std::__1::__wrap_iter.16"*, %"class.std::__1::__wrap_iter.16"** %4, align 8
  %8 = load %"class.std::__1::__wrap_iter"*, %"class.std::__1::__wrap_iter"** %5, align 8
  %9 = load i8*, i8** %6, align 8
  call void @_ZNSt3__111__wrap_iterIPKjEC2IPjEERKNS0_IT_EEPNS_9enable_ifIXsr14is_convertibleIS6_S2_EE5valueEvE4typeE(%"class.std::__1::__wrap_iter.16"* %7, %"class.std::__1::__wrap_iter"* nonnull align 8 dereferenceable(8) %8, i8* %9) #11
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i64 @_ZNSt3__123__libcpp_numeric_limitsImLb1EE3maxEv() #1 align 2 {
  ret i64 -1
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden zeroext i8 @_ZNSt3__123__libcpp_numeric_limitsIhLb1EE3maxEv() #1 align 2 {
  ret i8 -1
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i64 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE4sizeEv(%"class.std::__1::vector"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::vector"*, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %2, align 8
  %3 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %2, align 8
  %4 = bitcast %"class.std::__1::vector"* %3 to %"class.std::__1::__vector_base"*
  %5 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %4, i32 0, i32 1
  %6 = load i32*, i32** %5, align 8
  %7 = bitcast %"class.std::__1::vector"* %3 to %"class.std::__1::__vector_base"*
  %8 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %7, i32 0, i32 0
  %9 = load i32*, i32** %8, align 8
  %10 = ptrtoint i32* %6 to i64
  %11 = ptrtoint i32* %9 to i64
  %12 = sub i64 %10, %11
  %13 = sdiv exact i64 %12, 4
  ret i64 %13
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 4 dereferenceable(4) i32* @_ZNKSt3__16vectorIjNS_9allocatorIjEEEixEm(%"class.std::__1::vector"* %0, i64 %1) #1 align 2 {
  %3 = alloca %"class.std::__1::vector"*, align 8
  %4 = alloca i64, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %3, align 8
  store i64 %1, i64* %4, align 8
  %5 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  %6 = bitcast %"class.std::__1::vector"* %5 to %"class.std::__1::__vector_base"*
  %7 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %6, i32 0, i32 0
  %8 = load i32*, i32** %7, align 8
  %9 = load i64, i64* %4, align 8
  %10 = getelementptr inbounds i32, i32* %8, i64 %9
  ret i32* %10
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZN12CheckedRangeC2ERK8QirRangex(%struct.CheckedRange* %0, %struct.QirRange* nonnull align 8 dereferenceable(24) %1, i64 %2) unnamed_addr #0 align 2 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %4 = alloca %struct.CheckedRange*, align 8
  %5 = alloca %struct.QirRange*, align 8
  %6 = alloca i64, align 8
  %7 = alloca i8*, align 8
  %8 = alloca i32, align 4
  %9 = alloca i64, align 8
  %10 = alloca i64, align 8
  store %struct.CheckedRange* %0, %struct.CheckedRange** %4, align 8
  store %struct.QirRange* %1, %struct.QirRange** %5, align 8
  store i64 %2, i64* %6, align 8
  %11 = load %struct.CheckedRange*, %struct.CheckedRange** %4, align 8
  %12 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %13 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %12, i32 0, i32 0
  %14 = load i64, i64* %13, align 8
  %15 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %11, i32 0, i32 0
  store i64 %14, i64* %15, align 8
  %16 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %17 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %16, i32 0, i32 1
  %18 = load i64, i64* %17, align 8
  %19 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %11, i32 0, i32 1
  store i64 %18, i64* %19, align 8
  %20 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %21 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %20, i32 0, i32 1
  %22 = load i64, i64* %21, align 8
  %23 = icmp eq i64 %22, 0
  br i1 %23, label %24, label %32

24:                                               ; preds = %3
  %25 = call i8* @__cxa_allocate_exception(i64 16) #11
  %26 = bitcast i8* %25 to %"class.std::runtime_error"*
  invoke void @_ZNSt13runtime_errorC1EPKc(%"class.std::runtime_error"* %26, i8* getelementptr inbounds ([14 x i8], [14 x i8]* @.str.39, i64 0, i64 0))
          to label %27 unwind label %28

27:                                               ; preds = %24
  call void @__cxa_throw(i8* %25, i8* bitcast (i8** @_ZTISt13runtime_error to i8*), i8* bitcast (void (%"class.std::runtime_error"*)* @_ZNSt13runtime_errorD1Ev to i8*)) #18
  unreachable

28:                                               ; preds = %24
  %29 = landingpad { i8*, i32 }
          cleanup
  %30 = extractvalue { i8*, i32 } %29, 0
  store i8* %30, i8** %7, align 8
  %31 = extractvalue { i8*, i32 } %29, 1
  store i32 %31, i32* %8, align 4
  call void @__cxa_free_exception(i8* %25) #11
  br label %227

32:                                               ; preds = %3
  %33 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %34 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %33, i32 0, i32 1
  %35 = load i64, i64* %34, align 8
  %36 = icmp sgt i64 %35, 0
  br i1 %36, label %37, label %45

37:                                               ; preds = %32
  %38 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %39 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %38, i32 0, i32 2
  %40 = load i64, i64* %39, align 8
  %41 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %42 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %41, i32 0, i32 0
  %43 = load i64, i64* %42, align 8
  %44 = icmp slt i64 %40, %43
  br i1 %44, label %58, label %45

45:                                               ; preds = %37, %32
  %46 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %47 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %46, i32 0, i32 1
  %48 = load i64, i64* %47, align 8
  %49 = icmp slt i64 %48, 0
  br i1 %49, label %50, label %63

50:                                               ; preds = %45
  %51 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %52 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %51, i32 0, i32 0
  %53 = load i64, i64* %52, align 8
  %54 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %55 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %54, i32 0, i32 2
  %56 = load i64, i64* %55, align 8
  %57 = icmp slt i64 %53, %56
  br i1 %57, label %58, label %63

58:                                               ; preds = %50, %37
  %59 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %11, i32 0, i32 0
  store i64 0, i64* %59, align 8
  %60 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %11, i32 0, i32 1
  store i64 1, i64* %60, align 8
  %61 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %11, i32 0, i32 2
  store i64 0, i64* %61, align 8
  %62 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %11, i32 0, i32 3
  store i64 0, i64* %62, align 8
  br label %215

63:                                               ; preds = %50, %45
  %64 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %65 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %64, i32 0, i32 1
  %66 = load i64, i64* %65, align 8
  %67 = icmp sgt i64 %66, 0
  br i1 %67, label %68, label %141

68:                                               ; preds = %63
  %69 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %70 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %69, i32 0, i32 2
  %71 = load i64, i64* %70, align 8
  %72 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %73 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %72, i32 0, i32 0
  %74 = load i64, i64* %73, align 8
  %75 = sub nsw i64 %71, %74
  %76 = add nsw i64 %75, 1
  %77 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %78 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %77, i32 0, i32 1
  %79 = load i64, i64* %78, align 8
  %80 = sdiv i64 %76, %79
  %81 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %82 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %81, i32 0, i32 2
  %83 = load i64, i64* %82, align 8
  %84 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %85 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %84, i32 0, i32 0
  %86 = load i64, i64* %85, align 8
  %87 = sub nsw i64 %83, %86
  %88 = add nsw i64 %87, 1
  %89 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %90 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %89, i32 0, i32 1
  %91 = load i64, i64* %90, align 8
  %92 = srem i64 %88, %91
  %93 = icmp ne i64 %92, 0
  %94 = zext i1 %93 to i64
  %95 = select i1 %93, i32 1, i32 0
  %96 = sext i32 %95 to i64
  %97 = add nsw i64 %80, %96
  %98 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %11, i32 0, i32 3
  store i64 %97, i64* %98, align 8
  %99 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %11, i32 0, i32 3
  %100 = load i64, i64* %99, align 8
  %101 = icmp sgt i64 %100, 0
  %102 = xor i1 %101, true
  br i1 %102, label %103, label %105

103:                                              ; preds = %68
  call void @__assert_rtn(i8* getelementptr inbounds ([13 x i8], [13 x i8]* @__func__._ZN12CheckedRangeC2ERK8QirRangex, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 484, i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.str.40, i64 0, i64 0)) #14
  unreachable

104:                                              ; No predecessors!
  br label %106

105:                                              ; preds = %68
  br label %106

106:                                              ; preds = %105, %104
  %107 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %108 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %107, i32 0, i32 0
  %109 = load i64, i64* %108, align 8
  %110 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %11, i32 0, i32 3
  %111 = load i64, i64* %110, align 8
  %112 = sub nsw i64 %111, 1
  %113 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %114 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %113, i32 0, i32 1
  %115 = load i64, i64* %114, align 8
  %116 = mul nsw i64 %112, %115
  %117 = add nsw i64 %109, %116
  store i64 %117, i64* %9, align 8
  %118 = load i64, i64* %9, align 8
  %119 = load i64, i64* %6, align 8
  %120 = icmp sge i64 %118, %119
  br i1 %120, label %126, label %121

121:                                              ; preds = %106
  %122 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %123 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %122, i32 0, i32 0
  %124 = load i64, i64* %123, align 8
  %125 = icmp slt i64 %124, 0
  br i1 %125, label %126, label %134

126:                                              ; preds = %121, %106
  %127 = call i8* @__cxa_allocate_exception(i64 16) #11
  %128 = bitcast i8* %127 to %"class.std::runtime_error"*
  invoke void @_ZNSt13runtime_errorC1EPKc(%"class.std::runtime_error"* %128, i8* getelementptr inbounds ([20 x i8], [20 x i8]* @.str.41, i64 0, i64 0))
          to label %129 unwind label %130

129:                                              ; preds = %126
  call void @__cxa_throw(i8* %127, i8* bitcast (i8** @_ZTISt13runtime_error to i8*), i8* bitcast (void (%"class.std::runtime_error"*)* @_ZNSt13runtime_errorD1Ev to i8*)) #18
  unreachable

130:                                              ; preds = %126
  %131 = landingpad { i8*, i32 }
          cleanup
  %132 = extractvalue { i8*, i32 } %131, 0
  store i8* %132, i8** %7, align 8
  %133 = extractvalue { i8*, i32 } %131, 1
  store i32 %133, i32* %8, align 4
  call void @__cxa_free_exception(i8* %127) #11
  br label %227

134:                                              ; preds = %121
  %135 = load i64, i64* %9, align 8
  %136 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %137 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %136, i32 0, i32 1
  %138 = load i64, i64* %137, align 8
  %139 = add nsw i64 %135, %138
  %140 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %11, i32 0, i32 2
  store i64 %139, i64* %140, align 8
  br label %214

141:                                              ; preds = %63
  %142 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %143 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %142, i32 0, i32 2
  %144 = load i64, i64* %143, align 8
  %145 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %146 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %145, i32 0, i32 0
  %147 = load i64, i64* %146, align 8
  %148 = sub nsw i64 %144, %147
  %149 = sub nsw i64 %148, 1
  %150 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %151 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %150, i32 0, i32 1
  %152 = load i64, i64* %151, align 8
  %153 = sdiv i64 %149, %152
  %154 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %155 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %154, i32 0, i32 2
  %156 = load i64, i64* %155, align 8
  %157 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %158 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %157, i32 0, i32 0
  %159 = load i64, i64* %158, align 8
  %160 = sub nsw i64 %156, %159
  %161 = sub nsw i64 %160, 1
  %162 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %163 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %162, i32 0, i32 1
  %164 = load i64, i64* %163, align 8
  %165 = srem i64 %161, %164
  %166 = icmp ne i64 %165, 0
  %167 = zext i1 %166 to i64
  %168 = select i1 %166, i32 1, i32 0
  %169 = sext i32 %168 to i64
  %170 = add nsw i64 %153, %169
  %171 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %11, i32 0, i32 3
  store i64 %170, i64* %171, align 8
  %172 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %11, i32 0, i32 3
  %173 = load i64, i64* %172, align 8
  %174 = icmp sgt i64 %173, 0
  %175 = xor i1 %174, true
  br i1 %175, label %176, label %178

176:                                              ; preds = %141
  call void @__assert_rtn(i8* getelementptr inbounds ([13 x i8], [13 x i8]* @__func__._ZN12CheckedRangeC2ERK8QirRangex, i64 0, i64 0), i8* getelementptr inbounds ([80 x i8], [80 x i8]* @.str.1, i64 0, i64 0), i32 505, i8* getelementptr inbounds ([16 x i8], [16 x i8]* @.str.40, i64 0, i64 0)) #14
  unreachable

177:                                              ; No predecessors!
  br label %179

178:                                              ; preds = %141
  br label %179

179:                                              ; preds = %178, %177
  %180 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %181 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %180, i32 0, i32 0
  %182 = load i64, i64* %181, align 8
  %183 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %11, i32 0, i32 3
  %184 = load i64, i64* %183, align 8
  %185 = sub nsw i64 %184, 1
  %186 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %187 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %186, i32 0, i32 1
  %188 = load i64, i64* %187, align 8
  %189 = mul nsw i64 %185, %188
  %190 = add nsw i64 %182, %189
  store i64 %190, i64* %10, align 8
  %191 = load i64, i64* %10, align 8
  %192 = icmp slt i64 %191, 0
  br i1 %192, label %199, label %193

193:                                              ; preds = %179
  %194 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %195 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %194, i32 0, i32 0
  %196 = load i64, i64* %195, align 8
  %197 = load i64, i64* %6, align 8
  %198 = icmp sge i64 %196, %197
  br i1 %198, label %199, label %207

199:                                              ; preds = %193, %179
  %200 = call i8* @__cxa_allocate_exception(i64 16) #11
  %201 = bitcast i8* %200 to %"class.std::runtime_error"*
  invoke void @_ZNSt13runtime_errorC1EPKc(%"class.std::runtime_error"* %201, i8* getelementptr inbounds ([20 x i8], [20 x i8]* @.str.41, i64 0, i64 0))
          to label %202 unwind label %203

202:                                              ; preds = %199
  call void @__cxa_throw(i8* %200, i8* bitcast (i8** @_ZTISt13runtime_error to i8*), i8* bitcast (void (%"class.std::runtime_error"*)* @_ZNSt13runtime_errorD1Ev to i8*)) #18
  unreachable

203:                                              ; preds = %199
  %204 = landingpad { i8*, i32 }
          cleanup
  %205 = extractvalue { i8*, i32 } %204, 0
  store i8* %205, i8** %7, align 8
  %206 = extractvalue { i8*, i32 } %204, 1
  store i32 %206, i32* %8, align 4
  call void @__cxa_free_exception(i8* %200) #11
  br label %227

207:                                              ; preds = %193
  %208 = load i64, i64* %10, align 8
  %209 = load %struct.QirRange*, %struct.QirRange** %5, align 8
  %210 = getelementptr inbounds %struct.QirRange, %struct.QirRange* %209, i32 0, i32 1
  %211 = load i64, i64* %210, align 8
  %212 = add nsw i64 %208, %211
  %213 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %11, i32 0, i32 2
  store i64 %212, i64* %213, align 8
  br label %214

214:                                              ; preds = %207, %134
  br label %215

215:                                              ; preds = %214, %58
  br label %216

216:                                              ; preds = %215
  %217 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %11, i32 0, i32 3
  %218 = load i64, i64* %217, align 8
  %219 = icmp eq i64 %218, 1
  br i1 %219, label %220, label %226

220:                                              ; preds = %216
  %221 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %11, i32 0, i32 1
  store i64 1, i64* %221, align 8
  %222 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %11, i32 0, i32 0
  %223 = load i64, i64* %222, align 8
  %224 = add nsw i64 %223, 1
  %225 = getelementptr inbounds %struct.CheckedRange, %struct.CheckedRange* %11, i32 0, i32 2
  store i64 %224, i64* %225, align 8
  br label %226

226:                                              ; preds = %220, %216
  ret void

227:                                              ; preds = %203, %130, %28
  %228 = load i8*, i8** %7, align 8
  %229 = load i32, i32* %8, align 4
  %230 = insertvalue { i8*, i32 } undef, i8* %228, 0
  %231 = insertvalue { i8*, i32 } %230, i32 %229, 1
  resume { i8*, i32 } %231
}

declare i8* @__cxa_allocate_exception(i64)

declare void @_ZNSt13runtime_errorC1EPKc(%"class.std::runtime_error"*, i8*) unnamed_addr #2

declare void @__cxa_free_exception(i8*)

; Function Attrs: nounwind
declare void @_ZNSt13runtime_errorD1Ev(%"class.std::runtime_error"*) unnamed_addr #12

declare void @__cxa_throw(i8*, i8*, i8*)

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr hidden zeroext i8 @_ZNSt3__110accumulateINS_11__wrap_iterIPKjEEhNS_10multipliesIjEEEET0_T_S8_S7_T1_(i32* %0, i32* %1, i8 zeroext %2) #0 {
  %4 = alloca %"class.std::__1::__wrap_iter.16", align 8
  %5 = alloca %"class.std::__1::__wrap_iter.16", align 8
  %6 = alloca %"struct.std::__1::multiplies", align 1
  %7 = alloca i8, align 1
  %8 = alloca i32, align 4
  %9 = getelementptr inbounds %"class.std::__1::__wrap_iter.16", %"class.std::__1::__wrap_iter.16"* %4, i32 0, i32 0
  store i32* %0, i32** %9, align 8
  %10 = getelementptr inbounds %"class.std::__1::__wrap_iter.16", %"class.std::__1::__wrap_iter.16"* %5, i32 0, i32 0
  store i32* %1, i32** %10, align 8
  store i8 %2, i8* %7, align 1
  br label %11

11:                                               ; preds = %19, %3
  %12 = call zeroext i1 @_ZNSt3__1neIPKjEEbRKNS_11__wrap_iterIT_EES7_(%"class.std::__1::__wrap_iter.16"* nonnull align 8 dereferenceable(8) %4, %"class.std::__1::__wrap_iter.16"* nonnull align 8 dereferenceable(8) %5) #11
  br i1 %12, label %13, label %21

13:                                               ; preds = %11
  %14 = load i8, i8* %7, align 1
  %15 = zext i8 %14 to i32
  store i32 %15, i32* %8, align 4
  %16 = call nonnull align 4 dereferenceable(4) i32* @_ZNKSt3__111__wrap_iterIPKjEdeEv(%"class.std::__1::__wrap_iter.16"* %4) #11
  %17 = call i32 @_ZNKSt3__110multipliesIjEclERKjS3_(%"struct.std::__1::multiplies"* %6, i32* nonnull align 4 dereferenceable(4) %8, i32* nonnull align 4 dereferenceable(4) %16)
  %18 = trunc i32 %17 to i8
  store i8 %18, i8* %7, align 1
  br label %19

19:                                               ; preds = %13
  %20 = call nonnull align 8 dereferenceable(8) %"class.std::__1::__wrap_iter.16"* @_ZNSt3__111__wrap_iterIPKjEppEv(%"class.std::__1::__wrap_iter.16"* %4) #11
  br label %11

21:                                               ; preds = %11
  %22 = load i8, i8* %7, align 1
  ret i8 %22
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i32* @_ZNKSt3__16vectorIjNS_9allocatorIjEEE5beginEv(%"class.std::__1::vector"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::__wrap_iter.16", align 8
  %3 = alloca %"class.std::__1::vector"*, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %3, align 8
  %4 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  %5 = bitcast %"class.std::__1::vector"* %4 to %"class.std::__1::__vector_base"*
  %6 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %5, i32 0, i32 0
  %7 = load i32*, i32** %6, align 8
  %8 = call i32* @_ZNKSt3__16vectorIjNS_9allocatorIjEEE11__make_iterEPKj(%"class.std::__1::vector"* %4, i32* %7) #11
  %9 = getelementptr inbounds %"class.std::__1::__wrap_iter.16", %"class.std::__1::__wrap_iter.16"* %2, i32 0, i32 0
  store i32* %8, i32** %9, align 8
  %10 = getelementptr inbounds %"class.std::__1::__wrap_iter.16", %"class.std::__1::__wrap_iter.16"* %2, i32 0, i32 0
  %11 = load i32*, i32** %10, align 8
  ret i32* %11
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i32* @_ZNKSt3__111__wrap_iterIPKjEplEl(%"class.std::__1::__wrap_iter.16"* %0, i64 %1) #1 align 2 {
  %3 = alloca %"class.std::__1::__wrap_iter.16", align 8
  %4 = alloca %"class.std::__1::__wrap_iter.16"*, align 8
  %5 = alloca i64, align 8
  store %"class.std::__1::__wrap_iter.16"* %0, %"class.std::__1::__wrap_iter.16"** %4, align 8
  store i64 %1, i64* %5, align 8
  %6 = load %"class.std::__1::__wrap_iter.16"*, %"class.std::__1::__wrap_iter.16"** %4, align 8
  %7 = bitcast %"class.std::__1::__wrap_iter.16"* %3 to i8*
  %8 = bitcast %"class.std::__1::__wrap_iter.16"* %6 to i8*
  call void @llvm.memcpy.p0i8.p0i8.i64(i8* align 8 %7, i8* align 8 %8, i64 8, i1 false)
  %9 = load i64, i64* %5, align 8
  %10 = call nonnull align 8 dereferenceable(8) %"class.std::__1::__wrap_iter.16"* @_ZNSt3__111__wrap_iterIPKjEpLEl(%"class.std::__1::__wrap_iter.16"* %3, i64 %9) #11
  %11 = getelementptr inbounds %"class.std::__1::__wrap_iter.16", %"class.std::__1::__wrap_iter.16"* %3, i32 0, i32 0
  %12 = load i32*, i32** %11, align 8
  ret i32* %12
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i32* @_ZNKSt3__16vectorIjNS_9allocatorIjEEE3endEv(%"class.std::__1::vector"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::__wrap_iter.16", align 8
  %3 = alloca %"class.std::__1::vector"*, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %3, align 8
  %4 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  %5 = bitcast %"class.std::__1::vector"* %4 to %"class.std::__1::__vector_base"*
  %6 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %5, i32 0, i32 1
  %7 = load i32*, i32** %6, align 8
  %8 = call i32* @_ZNKSt3__16vectorIjNS_9allocatorIjEEE11__make_iterEPKj(%"class.std::__1::vector"* %4, i32* %7) #11
  %9 = getelementptr inbounds %"class.std::__1::__wrap_iter.16", %"class.std::__1::__wrap_iter.16"* %2, i32 0, i32 0
  store i32* %8, i32** %9, align 8
  %10 = getelementptr inbounds %"class.std::__1::__wrap_iter.16", %"class.std::__1::__wrap_iter.16"* %2, i32 0, i32 0
  %11 = load i32*, i32** %10, align 8
  ret i32* %11
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden zeroext i1 @_ZNSt3__1neIPKjEEbRKNS_11__wrap_iterIT_EES7_(%"class.std::__1::__wrap_iter.16"* nonnull align 8 dereferenceable(8) %0, %"class.std::__1::__wrap_iter.16"* nonnull align 8 dereferenceable(8) %1) #1 {
  %3 = alloca %"class.std::__1::__wrap_iter.16"*, align 8
  %4 = alloca %"class.std::__1::__wrap_iter.16"*, align 8
  store %"class.std::__1::__wrap_iter.16"* %0, %"class.std::__1::__wrap_iter.16"** %3, align 8
  store %"class.std::__1::__wrap_iter.16"* %1, %"class.std::__1::__wrap_iter.16"** %4, align 8
  %5 = load %"class.std::__1::__wrap_iter.16"*, %"class.std::__1::__wrap_iter.16"** %3, align 8
  %6 = load %"class.std::__1::__wrap_iter.16"*, %"class.std::__1::__wrap_iter.16"** %4, align 8
  %7 = call zeroext i1 @_ZNSt3__1eqIPKjS2_EEbRKNS_11__wrap_iterIT_EERKNS3_IT0_EE(%"class.std::__1::__wrap_iter.16"* nonnull align 8 dereferenceable(8) %5, %"class.std::__1::__wrap_iter.16"* nonnull align 8 dereferenceable(8) %6) #11
  %8 = xor i1 %7, true
  ret i1 %8
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i32 @_ZNKSt3__110multipliesIjEclERKjS3_(%"struct.std::__1::multiplies"* %0, i32* nonnull align 4 dereferenceable(4) %1, i32* nonnull align 4 dereferenceable(4) %2) #1 align 2 {
  %4 = alloca %"struct.std::__1::multiplies"*, align 8
  %5 = alloca i32*, align 8
  %6 = alloca i32*, align 8
  store %"struct.std::__1::multiplies"* %0, %"struct.std::__1::multiplies"** %4, align 8
  store i32* %1, i32** %5, align 8
  store i32* %2, i32** %6, align 8
  %7 = load %"struct.std::__1::multiplies"*, %"struct.std::__1::multiplies"** %4, align 8
  %8 = load i32*, i32** %5, align 8
  %9 = load i32, i32* %8, align 4
  %10 = load i32*, i32** %6, align 8
  %11 = load i32, i32* %10, align 4
  %12 = mul i32 %9, %11
  ret i32 %12
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 4 dereferenceable(4) i32* @_ZNKSt3__111__wrap_iterIPKjEdeEv(%"class.std::__1::__wrap_iter.16"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::__wrap_iter.16"*, align 8
  store %"class.std::__1::__wrap_iter.16"* %0, %"class.std::__1::__wrap_iter.16"** %2, align 8
  %3 = load %"class.std::__1::__wrap_iter.16"*, %"class.std::__1::__wrap_iter.16"** %2, align 8
  %4 = getelementptr inbounds %"class.std::__1::__wrap_iter.16", %"class.std::__1::__wrap_iter.16"* %3, i32 0, i32 0
  %5 = load i32*, i32** %4, align 8
  ret i32* %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) %"class.std::__1::__wrap_iter.16"* @_ZNSt3__111__wrap_iterIPKjEppEv(%"class.std::__1::__wrap_iter.16"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::__wrap_iter.16"*, align 8
  store %"class.std::__1::__wrap_iter.16"* %0, %"class.std::__1::__wrap_iter.16"** %2, align 8
  %3 = load %"class.std::__1::__wrap_iter.16"*, %"class.std::__1::__wrap_iter.16"** %2, align 8
  %4 = getelementptr inbounds %"class.std::__1::__wrap_iter.16", %"class.std::__1::__wrap_iter.16"* %3, i32 0, i32 0
  %5 = load i32*, i32** %4, align 8
  %6 = getelementptr inbounds i32, i32* %5, i32 1
  store i32* %6, i32** %4, align 8
  ret %"class.std::__1::__wrap_iter.16"* %3
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden zeroext i1 @_ZNSt3__1eqIPKjS2_EEbRKNS_11__wrap_iterIT_EERKNS3_IT0_EE(%"class.std::__1::__wrap_iter.16"* nonnull align 8 dereferenceable(8) %0, %"class.std::__1::__wrap_iter.16"* nonnull align 8 dereferenceable(8) %1) #1 {
  %3 = alloca %"class.std::__1::__wrap_iter.16"*, align 8
  %4 = alloca %"class.std::__1::__wrap_iter.16"*, align 8
  store %"class.std::__1::__wrap_iter.16"* %0, %"class.std::__1::__wrap_iter.16"** %3, align 8
  store %"class.std::__1::__wrap_iter.16"* %1, %"class.std::__1::__wrap_iter.16"** %4, align 8
  %5 = load %"class.std::__1::__wrap_iter.16"*, %"class.std::__1::__wrap_iter.16"** %3, align 8
  %6 = call i32* @_ZNKSt3__111__wrap_iterIPKjE4baseEv(%"class.std::__1::__wrap_iter.16"* %5) #11
  %7 = load %"class.std::__1::__wrap_iter.16"*, %"class.std::__1::__wrap_iter.16"** %4, align 8
  %8 = call i32* @_ZNKSt3__111__wrap_iterIPKjE4baseEv(%"class.std::__1::__wrap_iter.16"* %7) #11
  %9 = icmp eq i32* %6, %8
  ret i1 %9
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i32* @_ZNKSt3__111__wrap_iterIPKjE4baseEv(%"class.std::__1::__wrap_iter.16"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::__wrap_iter.16"*, align 8
  store %"class.std::__1::__wrap_iter.16"* %0, %"class.std::__1::__wrap_iter.16"** %2, align 8
  %3 = load %"class.std::__1::__wrap_iter.16"*, %"class.std::__1::__wrap_iter.16"** %2, align 8
  %4 = getelementptr inbounds %"class.std::__1::__wrap_iter.16", %"class.std::__1::__wrap_iter.16"* %3, i32 0, i32 0
  %5 = load i32*, i32** %4, align 8
  ret i32* %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i32* @_ZNKSt3__16vectorIjNS_9allocatorIjEEE11__make_iterEPKj(%"class.std::__1::vector"* %0, i32* %1) #1 align 2 {
  %3 = alloca %"class.std::__1::__wrap_iter.16", align 8
  %4 = alloca %"class.std::__1::vector"*, align 8
  %5 = alloca i32*, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %4, align 8
  store i32* %1, i32** %5, align 8
  %6 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %4, align 8
  %7 = load i32*, i32** %5, align 8
  call void @_ZNSt3__111__wrap_iterIPKjEC1ES2_(%"class.std::__1::__wrap_iter.16"* %3, i32* %7) #11
  %8 = getelementptr inbounds %"class.std::__1::__wrap_iter.16", %"class.std::__1::__wrap_iter.16"* %3, i32 0, i32 0
  %9 = load i32*, i32** %8, align 8
  ret i32* %9
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__111__wrap_iterIPKjEC1ES2_(%"class.std::__1::__wrap_iter.16"* %0, i32* %1) unnamed_addr #1 align 2 {
  %3 = alloca %"class.std::__1::__wrap_iter.16"*, align 8
  %4 = alloca i32*, align 8
  store %"class.std::__1::__wrap_iter.16"* %0, %"class.std::__1::__wrap_iter.16"** %3, align 8
  store i32* %1, i32** %4, align 8
  %5 = load %"class.std::__1::__wrap_iter.16"*, %"class.std::__1::__wrap_iter.16"** %3, align 8
  %6 = load i32*, i32** %4, align 8
  call void @_ZNSt3__111__wrap_iterIPKjEC2ES2_(%"class.std::__1::__wrap_iter.16"* %5, i32* %6) #11
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__111__wrap_iterIPKjEC2ES2_(%"class.std::__1::__wrap_iter.16"* %0, i32* %1) unnamed_addr #1 align 2 {
  %3 = alloca %"class.std::__1::__wrap_iter.16"*, align 8
  %4 = alloca i32*, align 8
  store %"class.std::__1::__wrap_iter.16"* %0, %"class.std::__1::__wrap_iter.16"** %3, align 8
  store i32* %1, i32** %4, align 8
  %5 = load %"class.std::__1::__wrap_iter.16"*, %"class.std::__1::__wrap_iter.16"** %3, align 8
  %6 = getelementptr inbounds %"class.std::__1::__wrap_iter.16", %"class.std::__1::__wrap_iter.16"* %5, i32 0, i32 0
  %7 = load i32*, i32** %4, align 8
  store i32* %7, i32** %6, align 8
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) %"class.std::__1::__wrap_iter.16"* @_ZNSt3__111__wrap_iterIPKjEpLEl(%"class.std::__1::__wrap_iter.16"* %0, i64 %1) #1 align 2 {
  %3 = alloca %"class.std::__1::__wrap_iter.16"*, align 8
  %4 = alloca i64, align 8
  store %"class.std::__1::__wrap_iter.16"* %0, %"class.std::__1::__wrap_iter.16"** %3, align 8
  store i64 %1, i64* %4, align 8
  %5 = load %"class.std::__1::__wrap_iter.16"*, %"class.std::__1::__wrap_iter.16"** %3, align 8
  %6 = load i64, i64* %4, align 8
  %7 = getelementptr inbounds %"class.std::__1::__wrap_iter.16", %"class.std::__1::__wrap_iter.16"* %5, i32 0, i32 0
  %8 = load i32*, i32** %7, align 8
  %9 = getelementptr inbounds i32, i32* %8, i64 %6
  store i32* %9, i32** %7, align 8
  ret %"class.std::__1::__wrap_iter.16"* %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) %"class.std::__1::__wrap_iter"* @_ZNSt3__111__wrap_iterIPjEpLEl(%"class.std::__1::__wrap_iter"* %0, i64 %1) #1 align 2 {
  %3 = alloca %"class.std::__1::__wrap_iter"*, align 8
  %4 = alloca i64, align 8
  store %"class.std::__1::__wrap_iter"* %0, %"class.std::__1::__wrap_iter"** %3, align 8
  store i64 %1, i64* %4, align 8
  %5 = load %"class.std::__1::__wrap_iter"*, %"class.std::__1::__wrap_iter"** %3, align 8
  %6 = load i64, i64* %4, align 8
  %7 = getelementptr inbounds %"class.std::__1::__wrap_iter", %"class.std::__1::__wrap_iter"* %5, i32 0, i32 0
  %8 = load i32*, i32** %7, align 8
  %9 = getelementptr inbounds i32, i32* %8, i64 %6
  store i32* %9, i32** %7, align 8
  ret %"class.std::__1::__wrap_iter"* %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZNSt3__111__wrap_iterIPKjEC2IPjEERKNS0_IT_EEPNS_9enable_ifIXsr14is_convertibleIS6_S2_EE5valueEvE4typeE(%"class.std::__1::__wrap_iter.16"* %0, %"class.std::__1::__wrap_iter"* nonnull align 8 dereferenceable(8) %1, i8* %2) unnamed_addr #1 align 2 {
  %4 = alloca %"class.std::__1::__wrap_iter.16"*, align 8
  %5 = alloca %"class.std::__1::__wrap_iter"*, align 8
  %6 = alloca i8*, align 8
  store %"class.std::__1::__wrap_iter.16"* %0, %"class.std::__1::__wrap_iter.16"** %4, align 8
  store %"class.std::__1::__wrap_iter"* %1, %"class.std::__1::__wrap_iter"** %5, align 8
  store i8* %2, i8** %6, align 8
  %7 = load %"class.std::__1::__wrap_iter.16"*, %"class.std::__1::__wrap_iter.16"** %4, align 8
  %8 = getelementptr inbounds %"class.std::__1::__wrap_iter.16", %"class.std::__1::__wrap_iter.16"* %7, i32 0, i32 0
  %9 = load %"class.std::__1::__wrap_iter"*, %"class.std::__1::__wrap_iter"** %5, align 8
  %10 = call i32* @_ZNKSt3__111__wrap_iterIPjE4baseEv(%"class.std::__1::__wrap_iter"* %9) #11
  store i32* %10, i32** %8, align 8
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i32* @_ZNKSt3__111__wrap_iterIPjE4baseEv(%"class.std::__1::__wrap_iter"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::__wrap_iter"*, align 8
  store %"class.std::__1::__wrap_iter"* %0, %"class.std::__1::__wrap_iter"** %2, align 8
  %3 = load %"class.std::__1::__wrap_iter"*, %"class.std::__1::__wrap_iter"** %2, align 8
  %4 = getelementptr inbounds %"class.std::__1::__wrap_iter", %"class.std::__1::__wrap_iter"* %3, i32 0, i32 0
  %5 = load i32*, i32** %4, align 8
  ret i32* %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden zeroext i1 @_ZNKSt3__110unique_ptrIN9Microsoft7Quantum19QirExecutionContextENS_14default_deleteIS3_EEEcvbEv(%"class.std::__1::unique_ptr"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::unique_ptr"*, align 8
  store %"class.std::__1::unique_ptr"* %0, %"class.std::__1::unique_ptr"** %2, align 8
  %3 = load %"class.std::__1::unique_ptr"*, %"class.std::__1::unique_ptr"** %2, align 8
  %4 = getelementptr inbounds %"class.std::__1::unique_ptr", %"class.std::__1::unique_ptr"* %3, i32 0, i32 0
  %5 = call nonnull align 8 dereferenceable(8) %"struct.Microsoft::Quantum::QirExecutionContext"** @_ZNKSt3__117__compressed_pairIPN9Microsoft7Quantum19QirExecutionContextENS_14default_deleteIS3_EEE5firstEv(%"class.std::__1::__compressed_pair.1"* %4) #11
  %6 = load %"struct.Microsoft::Quantum::QirExecutionContext"*, %"struct.Microsoft::Quantum::QirExecutionContext"** %5, align 8
  %7 = icmp ne %"struct.Microsoft::Quantum::QirExecutionContext"* %6, null
  ret i1 %7
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) %"struct.Microsoft::Quantum::QirExecutionContext"** @_ZNKSt3__117__compressed_pairIPN9Microsoft7Quantum19QirExecutionContextENS_14default_deleteIS3_EEE5firstEv(%"class.std::__1::__compressed_pair.1"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::__compressed_pair.1"*, align 8
  store %"class.std::__1::__compressed_pair.1"* %0, %"class.std::__1::__compressed_pair.1"** %2, align 8
  %3 = load %"class.std::__1::__compressed_pair.1"*, %"class.std::__1::__compressed_pair.1"** %2, align 8
  %4 = bitcast %"class.std::__1::__compressed_pair.1"* %3 to %"struct.std::__1::__compressed_pair_elem.2"*
  %5 = call nonnull align 8 dereferenceable(8) %"struct.Microsoft::Quantum::QirExecutionContext"** @_ZNKSt3__122__compressed_pair_elemIPN9Microsoft7Quantum19QirExecutionContextELi0ELb0EE5__getEv(%"struct.std::__1::__compressed_pair_elem.2"* %4) #11
  ret %"struct.Microsoft::Quantum::QirExecutionContext"** %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) %"struct.Microsoft::Quantum::QirExecutionContext"** @_ZNKSt3__122__compressed_pair_elemIPN9Microsoft7Quantum19QirExecutionContextELi0ELb0EE5__getEv(%"struct.std::__1::__compressed_pair_elem.2"* %0) #1 align 2 {
  %2 = alloca %"struct.std::__1::__compressed_pair_elem.2"*, align 8
  store %"struct.std::__1::__compressed_pair_elem.2"* %0, %"struct.std::__1::__compressed_pair_elem.2"** %2, align 8
  %3 = load %"struct.std::__1::__compressed_pair_elem.2"*, %"struct.std::__1::__compressed_pair_elem.2"** %2, align 8
  %4 = getelementptr inbounds %"struct.std::__1::__compressed_pair_elem.2", %"struct.std::__1::__compressed_pair_elem.2"* %3, i32 0, i32 0
  ret %"struct.Microsoft::Quantum::QirExecutionContext"** %4
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__16vectorIjNS_9allocatorIjEEEC2Ev(%"class.std::__1::vector"* %0) unnamed_addr #1 align 2 {
  %2 = alloca %"class.std::__1::vector"*, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %2, align 8
  %3 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %2, align 8
  %4 = bitcast %"class.std::__1::vector"* %3 to %"class.std::__1::__vector_base"*
  call void @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEEC2Ev(%"class.std::__1::__vector_base"* %4) #11
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEEC2Ev(%"class.std::__1::__vector_base"* %0) unnamed_addr #1 align 2 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %2 = alloca %"class.std::__1::__vector_base"*, align 8
  %3 = alloca i8*, align 8
  %4 = alloca %"struct.std::__1::__default_init_tag", align 1
  store %"class.std::__1::__vector_base"* %0, %"class.std::__1::__vector_base"** %2, align 8
  %5 = load %"class.std::__1::__vector_base"*, %"class.std::__1::__vector_base"** %2, align 8
  %6 = bitcast %"class.std::__1::__vector_base"* %5 to %"class.std::__1::__vector_base_common"*
  invoke void @_ZNSt3__120__vector_base_commonILb1EEC2Ev(%"class.std::__1::__vector_base_common"* %6)
          to label %7 unwind label %12

7:                                                ; preds = %1
  %8 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %5, i32 0, i32 0
  store i32* null, i32** %8, align 8
  %9 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %5, i32 0, i32 1
  store i32* null, i32** %9, align 8
  %10 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %5, i32 0, i32 2
  store i8* null, i8** %3, align 8
  invoke void @_ZNSt3__117__compressed_pairIPjNS_9allocatorIjEEEC1IDnNS_18__default_init_tagEEEOT_OT0_(%"class.std::__1::__compressed_pair"* %10, i8** nonnull align 8 dereferenceable(8) %3, %"struct.std::__1::__default_init_tag"* nonnull align 1 dereferenceable(1) %4)
          to label %11 unwind label %12

11:                                               ; preds = %7
  ret void

12:                                               ; preds = %7, %1
  %13 = landingpad { i8*, i32 }
          catch i8* null
  %14 = extractvalue { i8*, i32 } %13, 0
  call void @__clang_call_terminate(i8* %14) #17
  unreachable
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__120__vector_base_commonILb1EEC2Ev(%"class.std::__1::__vector_base_common"* %0) unnamed_addr #1 align 2 {
  %2 = alloca %"class.std::__1::__vector_base_common"*, align 8
  store %"class.std::__1::__vector_base_common"* %0, %"class.std::__1::__vector_base_common"** %2, align 8
  %3 = load %"class.std::__1::__vector_base_common"*, %"class.std::__1::__vector_base_common"** %2, align 8
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__117__compressed_pairIPjNS_9allocatorIjEEEC1IDnNS_18__default_init_tagEEEOT_OT0_(%"class.std::__1::__compressed_pair"* %0, i8** nonnull align 8 dereferenceable(8) %1, %"struct.std::__1::__default_init_tag"* nonnull align 1 dereferenceable(1) %2) unnamed_addr #0 align 2 {
  %4 = alloca %"class.std::__1::__compressed_pair"*, align 8
  %5 = alloca i8**, align 8
  %6 = alloca %"struct.std::__1::__default_init_tag"*, align 8
  store %"class.std::__1::__compressed_pair"* %0, %"class.std::__1::__compressed_pair"** %4, align 8
  store i8** %1, i8*** %5, align 8
  store %"struct.std::__1::__default_init_tag"* %2, %"struct.std::__1::__default_init_tag"** %6, align 8
  %7 = load %"class.std::__1::__compressed_pair"*, %"class.std::__1::__compressed_pair"** %4, align 8
  %8 = load i8**, i8*** %5, align 8
  %9 = load %"struct.std::__1::__default_init_tag"*, %"struct.std::__1::__default_init_tag"** %6, align 8
  call void @_ZNSt3__117__compressed_pairIPjNS_9allocatorIjEEEC2IDnNS_18__default_init_tagEEEOT_OT0_(%"class.std::__1::__compressed_pair"* %7, i8** nonnull align 8 dereferenceable(8) %8, %"struct.std::__1::__default_init_tag"* nonnull align 1 dereferenceable(1) %9)
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__117__compressed_pairIPjNS_9allocatorIjEEEC2IDnNS_18__default_init_tagEEEOT_OT0_(%"class.std::__1::__compressed_pair"* %0, i8** nonnull align 8 dereferenceable(8) %1, %"struct.std::__1::__default_init_tag"* nonnull align 1 dereferenceable(1) %2) unnamed_addr #0 align 2 {
  %4 = alloca %"class.std::__1::__compressed_pair"*, align 8
  %5 = alloca i8**, align 8
  %6 = alloca %"struct.std::__1::__default_init_tag"*, align 8
  %7 = alloca %"struct.std::__1::__default_init_tag", align 1
  store %"class.std::__1::__compressed_pair"* %0, %"class.std::__1::__compressed_pair"** %4, align 8
  store i8** %1, i8*** %5, align 8
  store %"struct.std::__1::__default_init_tag"* %2, %"struct.std::__1::__default_init_tag"** %6, align 8
  %8 = load %"class.std::__1::__compressed_pair"*, %"class.std::__1::__compressed_pair"** %4, align 8
  %9 = bitcast %"class.std::__1::__compressed_pair"* %8 to %"struct.std::__1::__compressed_pair_elem"*
  %10 = load i8**, i8*** %5, align 8
  %11 = call nonnull align 8 dereferenceable(8) i8** @_ZNSt3__17forwardIDnEEOT_RNS_16remove_referenceIS1_E4typeE(i8** nonnull align 8 dereferenceable(8) %10) #11
  call void @_ZNSt3__122__compressed_pair_elemIPjLi0ELb0EEC2IDnvEEOT_(%"struct.std::__1::__compressed_pair_elem"* %9, i8** nonnull align 8 dereferenceable(8) %11)
  %12 = bitcast %"class.std::__1::__compressed_pair"* %8 to %"struct.std::__1::__compressed_pair_elem.0"*
  %13 = load %"struct.std::__1::__default_init_tag"*, %"struct.std::__1::__default_init_tag"** %6, align 8
  %14 = call nonnull align 1 dereferenceable(1) %"struct.std::__1::__default_init_tag"* @_ZNSt3__17forwardINS_18__default_init_tagEEEOT_RNS_16remove_referenceIS2_E4typeE(%"struct.std::__1::__default_init_tag"* nonnull align 1 dereferenceable(1) %13) #11
  call void @_ZNSt3__122__compressed_pair_elemINS_9allocatorIjEELi1ELb1EEC2ENS_18__default_init_tagE(%"struct.std::__1::__compressed_pair_elem.0"* %12)
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) i8** @_ZNSt3__17forwardIDnEEOT_RNS_16remove_referenceIS1_E4typeE(i8** nonnull align 8 dereferenceable(8) %0) #1 {
  %2 = alloca i8**, align 8
  store i8** %0, i8*** %2, align 8
  %3 = load i8**, i8*** %2, align 8
  ret i8** %3
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZNSt3__122__compressed_pair_elemIPjLi0ELb0EEC2IDnvEEOT_(%"struct.std::__1::__compressed_pair_elem"* %0, i8** nonnull align 8 dereferenceable(8) %1) unnamed_addr #1 align 2 {
  %3 = alloca %"struct.std::__1::__compressed_pair_elem"*, align 8
  %4 = alloca i8**, align 8
  store %"struct.std::__1::__compressed_pair_elem"* %0, %"struct.std::__1::__compressed_pair_elem"** %3, align 8
  store i8** %1, i8*** %4, align 8
  %5 = load %"struct.std::__1::__compressed_pair_elem"*, %"struct.std::__1::__compressed_pair_elem"** %3, align 8
  %6 = getelementptr inbounds %"struct.std::__1::__compressed_pair_elem", %"struct.std::__1::__compressed_pair_elem"* %5, i32 0, i32 0
  %7 = load i8**, i8*** %4, align 8
  %8 = call nonnull align 8 dereferenceable(8) i8** @_ZNSt3__17forwardIDnEEOT_RNS_16remove_referenceIS1_E4typeE(i8** nonnull align 8 dereferenceable(8) %7) #11
  store i32* null, i32** %6, align 8
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 1 dereferenceable(1) %"struct.std::__1::__default_init_tag"* @_ZNSt3__17forwardINS_18__default_init_tagEEEOT_RNS_16remove_referenceIS2_E4typeE(%"struct.std::__1::__default_init_tag"* nonnull align 1 dereferenceable(1) %0) #1 {
  %2 = alloca %"struct.std::__1::__default_init_tag"*, align 8
  store %"struct.std::__1::__default_init_tag"* %0, %"struct.std::__1::__default_init_tag"** %2, align 8
  %3 = load %"struct.std::__1::__default_init_tag"*, %"struct.std::__1::__default_init_tag"** %2, align 8
  ret %"struct.std::__1::__default_init_tag"* %3
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__122__compressed_pair_elemINS_9allocatorIjEELi1ELb1EEC2ENS_18__default_init_tagE(%"struct.std::__1::__compressed_pair_elem.0"* %0) unnamed_addr #1 align 2 {
  %2 = alloca %"struct.std::__1::__default_init_tag", align 1
  %3 = alloca %"struct.std::__1::__compressed_pair_elem.0"*, align 8
  store %"struct.std::__1::__compressed_pair_elem.0"* %0, %"struct.std::__1::__compressed_pair_elem.0"** %3, align 8
  %4 = load %"struct.std::__1::__compressed_pair_elem.0"*, %"struct.std::__1::__compressed_pair_elem.0"** %3, align 8
  %5 = bitcast %"struct.std::__1::__compressed_pair_elem.0"* %4 to %"class.std::__1::allocator"*
  call void @_ZNSt3__19allocatorIjEC2Ev(%"class.std::__1::allocator"* %5) #11
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__19allocatorIjEC2Ev(%"class.std::__1::allocator"* %0) unnamed_addr #1 align 2 {
  %2 = alloca %"class.std::__1::allocator"*, align 8
  store %"class.std::__1::allocator"* %0, %"class.std::__1::allocator"** %2, align 8
  %3 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %2, align 8
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__16vectorIjNS_9allocatorIjEEED2Ev(%"class.std::__1::vector"* %0) unnamed_addr #1 align 2 {
  %2 = alloca %"class.std::__1::vector"*, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %2, align 8
  %3 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %2, align 8
  call void @_ZNKSt3__16vectorIjNS_9allocatorIjEEE17__annotate_deleteEv(%"class.std::__1::vector"* %3) #11
  %4 = bitcast %"class.std::__1::vector"* %3 to %"class.std::__1::__vector_base"*
  call void @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEED2Ev(%"class.std::__1::__vector_base"* %4) #11
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNKSt3__16vectorIjNS_9allocatorIjEEE17__annotate_deleteEv(%"class.std::__1::vector"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::vector"*, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %2, align 8
  %3 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %2, align 8
  %4 = call i32* @_ZNKSt3__16vectorIjNS_9allocatorIjEEE4dataEv(%"class.std::__1::vector"* %3) #11
  %5 = bitcast i32* %4 to i8*
  %6 = call i32* @_ZNKSt3__16vectorIjNS_9allocatorIjEEE4dataEv(%"class.std::__1::vector"* %3) #11
  %7 = call i64 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE8capacityEv(%"class.std::__1::vector"* %3) #11
  %8 = getelementptr inbounds i32, i32* %6, i64 %7
  %9 = bitcast i32* %8 to i8*
  %10 = call i32* @_ZNKSt3__16vectorIjNS_9allocatorIjEEE4dataEv(%"class.std::__1::vector"* %3) #11
  %11 = call i64 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE4sizeEv(%"class.std::__1::vector"* %3) #11
  %12 = getelementptr inbounds i32, i32* %10, i64 %11
  %13 = bitcast i32* %12 to i8*
  %14 = call i32* @_ZNKSt3__16vectorIjNS_9allocatorIjEEE4dataEv(%"class.std::__1::vector"* %3) #11
  %15 = call i64 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE8capacityEv(%"class.std::__1::vector"* %3) #11
  %16 = getelementptr inbounds i32, i32* %14, i64 %15
  %17 = bitcast i32* %16 to i8*
  call void @_ZNKSt3__16vectorIjNS_9allocatorIjEEE31__annotate_contiguous_containerEPKvS5_S5_S5_(%"class.std::__1::vector"* %3, i8* %5, i8* %9, i8* %13, i8* %17) #11
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEED2Ev(%"class.std::__1::__vector_base"* %0) unnamed_addr #1 align 2 {
  %2 = alloca %"class.std::__1::__vector_base"*, align 8
  store %"class.std::__1::__vector_base"* %0, %"class.std::__1::__vector_base"** %2, align 8
  %3 = load %"class.std::__1::__vector_base"*, %"class.std::__1::__vector_base"** %2, align 8
  %4 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %3, i32 0, i32 0
  %5 = load i32*, i32** %4, align 8
  %6 = icmp ne i32* %5, null
  br i1 %6, label %7, label %12

7:                                                ; preds = %1
  call void @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEE5clearEv(%"class.std::__1::__vector_base"* %3) #11
  %8 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEE7__allocEv(%"class.std::__1::__vector_base"* %3) #11
  %9 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %3, i32 0, i32 0
  %10 = load i32*, i32** %9, align 8
  %11 = call i64 @_ZNKSt3__113__vector_baseIjNS_9allocatorIjEEE8capacityEv(%"class.std::__1::__vector_base"* %3) #11
  call void @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE10deallocateERS2_Pjm(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %8, i32* %10, i64 %11) #11
  br label %12

12:                                               ; preds = %7, %1
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNKSt3__16vectorIjNS_9allocatorIjEEE31__annotate_contiguous_containerEPKvS5_S5_S5_(%"class.std::__1::vector"* %0, i8* %1, i8* %2, i8* %3, i8* %4) #1 align 2 {
  %6 = alloca %"class.std::__1::vector"*, align 8
  %7 = alloca i8*, align 8
  %8 = alloca i8*, align 8
  %9 = alloca i8*, align 8
  %10 = alloca i8*, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %6, align 8
  store i8* %1, i8** %7, align 8
  store i8* %2, i8** %8, align 8
  store i8* %3, i8** %9, align 8
  store i8* %4, i8** %10, align 8
  %11 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %6, align 8
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i32* @_ZNKSt3__16vectorIjNS_9allocatorIjEEE4dataEv(%"class.std::__1::vector"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::vector"*, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %2, align 8
  %3 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %2, align 8
  %4 = bitcast %"class.std::__1::vector"* %3 to %"class.std::__1::__vector_base"*
  %5 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %4, i32 0, i32 0
  %6 = load i32*, i32** %5, align 8
  %7 = call i32* @_ZNSt3__112__to_addressIjEEPT_S2_(i32* %6) #11
  ret i32* %7
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i64 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE8capacityEv(%"class.std::__1::vector"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::vector"*, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %2, align 8
  %3 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %2, align 8
  %4 = bitcast %"class.std::__1::vector"* %3 to %"class.std::__1::__vector_base"*
  %5 = call i64 @_ZNKSt3__113__vector_baseIjNS_9allocatorIjEEE8capacityEv(%"class.std::__1::__vector_base"* %4) #11
  ret i64 %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i32* @_ZNSt3__112__to_addressIjEEPT_S2_(i32* %0) #1 {
  %2 = alloca i32*, align 8
  store i32* %0, i32** %2, align 8
  %3 = load i32*, i32** %2, align 8
  ret i32* %3
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i64 @_ZNKSt3__113__vector_baseIjNS_9allocatorIjEEE8capacityEv(%"class.std::__1::__vector_base"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::__vector_base"*, align 8
  store %"class.std::__1::__vector_base"* %0, %"class.std::__1::__vector_base"** %2, align 8
  %3 = load %"class.std::__1::__vector_base"*, %"class.std::__1::__vector_base"** %2, align 8
  %4 = call nonnull align 8 dereferenceable(8) i32** @_ZNKSt3__113__vector_baseIjNS_9allocatorIjEEE9__end_capEv(%"class.std::__1::__vector_base"* %3) #11
  %5 = load i32*, i32** %4, align 8
  %6 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %3, i32 0, i32 0
  %7 = load i32*, i32** %6, align 8
  %8 = ptrtoint i32* %5 to i64
  %9 = ptrtoint i32* %7 to i64
  %10 = sub i64 %8, %9
  %11 = sdiv exact i64 %10, 4
  ret i64 %11
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) i32** @_ZNKSt3__113__vector_baseIjNS_9allocatorIjEEE9__end_capEv(%"class.std::__1::__vector_base"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::__vector_base"*, align 8
  store %"class.std::__1::__vector_base"* %0, %"class.std::__1::__vector_base"** %2, align 8
  %3 = load %"class.std::__1::__vector_base"*, %"class.std::__1::__vector_base"** %2, align 8
  %4 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %3, i32 0, i32 2
  %5 = call nonnull align 8 dereferenceable(8) i32** @_ZNKSt3__117__compressed_pairIPjNS_9allocatorIjEEE5firstEv(%"class.std::__1::__compressed_pair"* %4) #11
  ret i32** %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) i32** @_ZNKSt3__117__compressed_pairIPjNS_9allocatorIjEEE5firstEv(%"class.std::__1::__compressed_pair"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::__compressed_pair"*, align 8
  store %"class.std::__1::__compressed_pair"* %0, %"class.std::__1::__compressed_pair"** %2, align 8
  %3 = load %"class.std::__1::__compressed_pair"*, %"class.std::__1::__compressed_pair"** %2, align 8
  %4 = bitcast %"class.std::__1::__compressed_pair"* %3 to %"struct.std::__1::__compressed_pair_elem"*
  %5 = call nonnull align 8 dereferenceable(8) i32** @_ZNKSt3__122__compressed_pair_elemIPjLi0ELb0EE5__getEv(%"struct.std::__1::__compressed_pair_elem"* %4) #11
  ret i32** %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) i32** @_ZNKSt3__122__compressed_pair_elemIPjLi0ELb0EE5__getEv(%"struct.std::__1::__compressed_pair_elem"* %0) #1 align 2 {
  %2 = alloca %"struct.std::__1::__compressed_pair_elem"*, align 8
  store %"struct.std::__1::__compressed_pair_elem"* %0, %"struct.std::__1::__compressed_pair_elem"** %2, align 8
  %3 = load %"struct.std::__1::__compressed_pair_elem"*, %"struct.std::__1::__compressed_pair_elem"** %2, align 8
  %4 = getelementptr inbounds %"struct.std::__1::__compressed_pair_elem", %"struct.std::__1::__compressed_pair_elem"* %3, i32 0, i32 0
  ret i32** %4
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEE5clearEv(%"class.std::__1::__vector_base"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::__vector_base"*, align 8
  store %"class.std::__1::__vector_base"* %0, %"class.std::__1::__vector_base"** %2, align 8
  %3 = load %"class.std::__1::__vector_base"*, %"class.std::__1::__vector_base"** %2, align 8
  %4 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %3, i32 0, i32 0
  %5 = load i32*, i32** %4, align 8
  call void @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEE17__destruct_at_endEPj(%"class.std::__1::__vector_base"* %3, i32* %5) #11
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE10deallocateERS2_Pjm(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %0, i32* %1, i64 %2) #1 align 2 {
  %4 = alloca %"class.std::__1::allocator"*, align 8
  %5 = alloca i32*, align 8
  %6 = alloca i64, align 8
  store %"class.std::__1::allocator"* %0, %"class.std::__1::allocator"** %4, align 8
  store i32* %1, i32** %5, align 8
  store i64 %2, i64* %6, align 8
  %7 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %4, align 8
  %8 = load i32*, i32** %5, align 8
  %9 = load i64, i64* %6, align 8
  call void @_ZNSt3__19allocatorIjE10deallocateEPjm(%"class.std::__1::allocator"* %7, i32* %8, i64 %9) #11
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEE7__allocEv(%"class.std::__1::__vector_base"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::__vector_base"*, align 8
  store %"class.std::__1::__vector_base"* %0, %"class.std::__1::__vector_base"** %2, align 8
  %3 = load %"class.std::__1::__vector_base"*, %"class.std::__1::__vector_base"** %2, align 8
  %4 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %3, i32 0, i32 2
  %5 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__117__compressed_pairIPjNS_9allocatorIjEEE6secondEv(%"class.std::__1::__compressed_pair"* %4) #11
  ret %"class.std::__1::allocator"* %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEE17__destruct_at_endEPj(%"class.std::__1::__vector_base"* %0, i32* %1) #1 align 2 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %3 = alloca %"class.std::__1::__vector_base"*, align 8
  %4 = alloca i32*, align 8
  %5 = alloca i32*, align 8
  store %"class.std::__1::__vector_base"* %0, %"class.std::__1::__vector_base"** %3, align 8
  store i32* %1, i32** %4, align 8
  %6 = load %"class.std::__1::__vector_base"*, %"class.std::__1::__vector_base"** %3, align 8
  %7 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %6, i32 0, i32 1
  %8 = load i32*, i32** %7, align 8
  store i32* %8, i32** %5, align 8
  br label %9

9:                                                ; preds = %18, %2
  %10 = load i32*, i32** %4, align 8
  %11 = load i32*, i32** %5, align 8
  %12 = icmp ne i32* %10, %11
  br i1 %12, label %13, label %19

13:                                               ; preds = %9
  %14 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEE7__allocEv(%"class.std::__1::__vector_base"* %6) #11
  %15 = load i32*, i32** %5, align 8
  %16 = getelementptr inbounds i32, i32* %15, i32 -1
  store i32* %16, i32** %5, align 8
  %17 = call i32* @_ZNSt3__112__to_addressIjEEPT_S2_(i32* %16) #11
  invoke void @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE7destroyIjEEvRS2_PT_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %14, i32* %17)
          to label %18 unwind label %22

18:                                               ; preds = %13
  br label %9

19:                                               ; preds = %9
  %20 = load i32*, i32** %4, align 8
  %21 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %6, i32 0, i32 1
  store i32* %20, i32** %21, align 8
  ret void

22:                                               ; preds = %13
  %23 = landingpad { i8*, i32 }
          catch i8* null
  %24 = extractvalue { i8*, i32 } %23, 0
  call void @__clang_call_terminate(i8* %24) #17
  unreachable
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE7destroyIjEEvRS2_PT_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %0, i32* %1) #0 align 2 {
  %3 = alloca %"class.std::__1::allocator"*, align 8
  %4 = alloca i32*, align 8
  %5 = alloca %"struct.std::__1::integral_constant", align 1
  %6 = alloca %"struct.std::__1::__has_destroy", align 1
  store %"class.std::__1::allocator"* %0, %"class.std::__1::allocator"** %3, align 8
  store i32* %1, i32** %4, align 8
  %7 = bitcast %"struct.std::__1::__has_destroy"* %6 to %"struct.std::__1::integral_constant"*
  %8 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %3, align 8
  %9 = load i32*, i32** %4, align 8
  call void @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE9__destroyIjEEvNS_17integral_constantIbLb1EEERS2_PT_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %8, i32* %9)
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE9__destroyIjEEvNS_17integral_constantIbLb1EEERS2_PT_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %0, i32* %1) #0 align 2 {
  %3 = alloca %"struct.std::__1::integral_constant", align 1
  %4 = alloca %"class.std::__1::allocator"*, align 8
  %5 = alloca i32*, align 8
  store %"class.std::__1::allocator"* %0, %"class.std::__1::allocator"** %4, align 8
  store i32* %1, i32** %5, align 8
  %6 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %4, align 8
  %7 = load i32*, i32** %5, align 8
  call void @_ZNSt3__19allocatorIjE7destroyEPj(%"class.std::__1::allocator"* %6, i32* %7)
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__19allocatorIjE7destroyEPj(%"class.std::__1::allocator"* %0, i32* %1) #1 align 2 {
  %3 = alloca %"class.std::__1::allocator"*, align 8
  %4 = alloca i32*, align 8
  store %"class.std::__1::allocator"* %0, %"class.std::__1::allocator"** %3, align 8
  store i32* %1, i32** %4, align 8
  %5 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %3, align 8
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__19allocatorIjE10deallocateEPjm(%"class.std::__1::allocator"* %0, i32* %1, i64 %2) #1 align 2 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %4 = alloca %"class.std::__1::allocator"*, align 8
  %5 = alloca i32*, align 8
  %6 = alloca i64, align 8
  store %"class.std::__1::allocator"* %0, %"class.std::__1::allocator"** %4, align 8
  store i32* %1, i32** %5, align 8
  store i64 %2, i64* %6, align 8
  %7 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %4, align 8
  %8 = load i32*, i32** %5, align 8
  %9 = bitcast i32* %8 to i8*
  %10 = load i64, i64* %6, align 8
  %11 = mul i64 %10, 4
  invoke void @_ZNSt3__119__libcpp_deallocateEPvmm(i8* %9, i64 %11, i64 4)
          to label %12 unwind label %13

12:                                               ; preds = %3
  ret void

13:                                               ; preds = %3
  %14 = landingpad { i8*, i32 }
          catch i8* null
  %15 = extractvalue { i8*, i32 } %14, 0
  call void @__clang_call_terminate(i8* %15) #17
  unreachable
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__119__libcpp_deallocateEPvmm(i8* %0, i64 %1, i64 %2) #0 {
  %4 = alloca i8*, align 8
  %5 = alloca i64, align 8
  %6 = alloca i64, align 8
  store i8* %0, i8** %4, align 8
  store i64 %1, i64* %5, align 8
  store i64 %2, i64* %6, align 8
  %7 = load i8*, i8** %4, align 8
  %8 = load i64, i64* %5, align 8
  %9 = load i64, i64* %6, align 8
  call void @_ZNSt3__117_DeallocateCaller33__do_deallocate_handle_size_alignEPvmm(i8* %7, i64 %8, i64 %9)
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__117_DeallocateCaller33__do_deallocate_handle_size_alignEPvmm(i8* %0, i64 %1, i64 %2) #0 align 2 {
  %4 = alloca i8*, align 8
  %5 = alloca i64, align 8
  %6 = alloca i64, align 8
  store i8* %0, i8** %4, align 8
  store i64 %1, i64* %5, align 8
  store i64 %2, i64* %6, align 8
  %7 = load i8*, i8** %4, align 8
  %8 = load i64, i64* %5, align 8
  call void @_ZNSt3__117_DeallocateCaller27__do_deallocate_handle_sizeEPvm(i8* %7, i64 %8)
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__117_DeallocateCaller27__do_deallocate_handle_sizeEPvm(i8* %0, i64 %1) #0 align 2 {
  %3 = alloca i8*, align 8
  %4 = alloca i64, align 8
  store i8* %0, i8** %3, align 8
  store i64 %1, i64* %4, align 8
  %5 = load i8*, i8** %3, align 8
  call void @_ZNSt3__117_DeallocateCaller9__do_callEPv(i8* %5)
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZNSt3__117_DeallocateCaller9__do_callEPv(i8* %0) #1 align 2 {
  %2 = alloca i8*, align 8
  store i8* %0, i8** %2, align 8
  %3 = load i8*, i8** %2, align 8
  call void @_ZdlPv(i8* %3) #15
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__117__compressed_pairIPjNS_9allocatorIjEEE6secondEv(%"class.std::__1::__compressed_pair"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::__compressed_pair"*, align 8
  store %"class.std::__1::__compressed_pair"* %0, %"class.std::__1::__compressed_pair"** %2, align 8
  %3 = load %"class.std::__1::__compressed_pair"*, %"class.std::__1::__compressed_pair"** %2, align 8
  %4 = bitcast %"class.std::__1::__compressed_pair"* %3 to %"struct.std::__1::__compressed_pair_elem.0"*
  %5 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__122__compressed_pair_elemINS_9allocatorIjEELi1ELb1EE5__getEv(%"struct.std::__1::__compressed_pair_elem.0"* %4) #11
  ret %"class.std::__1::allocator"* %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__122__compressed_pair_elemINS_9allocatorIjEELi1ELb1EE5__getEv(%"struct.std::__1::__compressed_pair_elem.0"* %0) #1 align 2 {
  %2 = alloca %"struct.std::__1::__compressed_pair_elem.0"*, align 8
  store %"struct.std::__1::__compressed_pair_elem.0"* %0, %"struct.std::__1::__compressed_pair_elem.0"** %2, align 8
  %3 = load %"struct.std::__1::__compressed_pair_elem.0"*, %"struct.std::__1::__compressed_pair_elem.0"** %2, align 8
  %4 = bitcast %"struct.std::__1::__compressed_pair_elem.0"* %3 to %"class.std::__1::allocator"*
  ret %"class.std::__1::allocator"* %4
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) i32** @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEE9__end_capEv(%"class.std::__1::__vector_base"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::__vector_base"*, align 8
  store %"class.std::__1::__vector_base"* %0, %"class.std::__1::__vector_base"** %2, align 8
  %3 = load %"class.std::__1::__vector_base"*, %"class.std::__1::__vector_base"** %2, align 8
  %4 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %3, i32 0, i32 2
  %5 = call nonnull align 8 dereferenceable(8) i32** @_ZNSt3__117__compressed_pairIPjNS_9allocatorIjEEE5firstEv(%"class.std::__1::__compressed_pair"* %4) #11
  ret i32** %5
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__16vectorIjNS_9allocatorIjEEE22__construct_one_at_endIJRKjEEEvDpOT_(%"class.std::__1::vector"* %0, i32* nonnull align 4 dereferenceable(4) %1) #0 align 2 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %3 = alloca %"class.std::__1::vector"*, align 8
  %4 = alloca i32*, align 8
  %5 = alloca %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction", align 8
  %6 = alloca i8*, align 8
  %7 = alloca i32, align 4
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %3, align 8
  store i32* %1, i32** %4, align 8
  %8 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEE21_ConstructTransactionC1ERS3_m(%"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %5, %"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %8, i64 1)
  %9 = bitcast %"class.std::__1::vector"* %8 to %"class.std::__1::__vector_base"*
  %10 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEE7__allocEv(%"class.std::__1::__vector_base"* %9) #11
  %11 = getelementptr inbounds %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction", %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %5, i32 0, i32 1
  %12 = load i32*, i32** %11, align 8
  %13 = call i32* @_ZNSt3__112__to_addressIjEEPT_S2_(i32* %12) #11
  %14 = load i32*, i32** %4, align 8
  %15 = call nonnull align 4 dereferenceable(4) i32* @_ZNSt3__17forwardIRKjEEOT_RNS_16remove_referenceIS3_E4typeE(i32* nonnull align 4 dereferenceable(4) %14) #11
  invoke void @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE9constructIjJRKjEEEvRS2_PT_DpOT0_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %10, i32* %13, i32* nonnull align 4 dereferenceable(4) %15)
          to label %16 unwind label %20

16:                                               ; preds = %2
  %17 = getelementptr inbounds %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction", %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %5, i32 0, i32 1
  %18 = load i32*, i32** %17, align 8
  %19 = getelementptr inbounds i32, i32* %18, i32 1
  store i32* %19, i32** %17, align 8
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEE21_ConstructTransactionD1Ev(%"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %5) #11
  ret void

20:                                               ; preds = %2
  %21 = landingpad { i8*, i32 }
          cleanup
  %22 = extractvalue { i8*, i32 } %21, 0
  store i8* %22, i8** %6, align 8
  %23 = extractvalue { i8*, i32 } %21, 1
  store i32 %23, i32* %7, align 4
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEE21_ConstructTransactionD1Ev(%"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %5) #11
  br label %24

24:                                               ; preds = %20
  %25 = load i8*, i8** %6, align 8
  %26 = load i32, i32* %7, align 4
  %27 = insertvalue { i8*, i32 } undef, i8* %25, 0
  %28 = insertvalue { i8*, i32 } %27, i32 %26, 1
  resume { i8*, i32 } %28
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__16vectorIjNS_9allocatorIjEEE21__push_back_slow_pathIRKjEEvOT_(%"class.std::__1::vector"* %0, i32* nonnull align 4 dereferenceable(4) %1) #0 align 2 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %3 = alloca %"class.std::__1::vector"*, align 8
  %4 = alloca i32*, align 8
  %5 = alloca %"class.std::__1::allocator"*, align 8
  %6 = alloca %"struct.std::__1::__split_buffer", align 8
  %7 = alloca i8*, align 8
  %8 = alloca i32, align 4
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %3, align 8
  store i32* %1, i32** %4, align 8
  %9 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  %10 = bitcast %"class.std::__1::vector"* %9 to %"class.std::__1::__vector_base"*
  %11 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEE7__allocEv(%"class.std::__1::__vector_base"* %10) #11
  store %"class.std::__1::allocator"* %11, %"class.std::__1::allocator"** %5, align 8
  %12 = call i64 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE4sizeEv(%"class.std::__1::vector"* %9) #11
  %13 = add i64 %12, 1
  %14 = call i64 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE11__recommendEm(%"class.std::__1::vector"* %9, i64 %13)
  %15 = call i64 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE4sizeEv(%"class.std::__1::vector"* %9) #11
  %16 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %5, align 8
  call void @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEEC1EmmS3_(%"struct.std::__1::__split_buffer"* %6, i64 %14, i64 %15, %"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %16)
  %17 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %5, align 8
  %18 = getelementptr inbounds %"struct.std::__1::__split_buffer", %"struct.std::__1::__split_buffer"* %6, i32 0, i32 2
  %19 = load i32*, i32** %18, align 8
  %20 = call i32* @_ZNSt3__112__to_addressIjEEPT_S2_(i32* %19) #11
  %21 = load i32*, i32** %4, align 8
  %22 = call nonnull align 4 dereferenceable(4) i32* @_ZNSt3__17forwardIRKjEEOT_RNS_16remove_referenceIS3_E4typeE(i32* nonnull align 4 dereferenceable(4) %21) #11
  invoke void @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE9constructIjJRKjEEEvRS2_PT_DpOT0_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %17, i32* %20, i32* nonnull align 4 dereferenceable(4) %22)
          to label %23 unwind label %28

23:                                               ; preds = %2
  %24 = getelementptr inbounds %"struct.std::__1::__split_buffer", %"struct.std::__1::__split_buffer"* %6, i32 0, i32 2
  %25 = load i32*, i32** %24, align 8
  %26 = getelementptr inbounds i32, i32* %25, i32 1
  store i32* %26, i32** %24, align 8
  invoke void @_ZNSt3__16vectorIjNS_9allocatorIjEEE26__swap_out_circular_bufferERNS_14__split_bufferIjRS2_EE(%"class.std::__1::vector"* %9, %"struct.std::__1::__split_buffer"* nonnull align 8 dereferenceable(40) %6)
          to label %27 unwind label %28

27:                                               ; preds = %23
  call void @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEED1Ev(%"struct.std::__1::__split_buffer"* %6) #11
  ret void

28:                                               ; preds = %23, %2
  %29 = landingpad { i8*, i32 }
          cleanup
  %30 = extractvalue { i8*, i32 } %29, 0
  store i8* %30, i8** %7, align 8
  %31 = extractvalue { i8*, i32 } %29, 1
  store i32 %31, i32* %8, align 4
  call void @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEED1Ev(%"struct.std::__1::__split_buffer"* %6) #11
  br label %32

32:                                               ; preds = %28
  %33 = load i8*, i8** %7, align 8
  %34 = load i32, i32* %8, align 4
  %35 = insertvalue { i8*, i32 } undef, i8* %33, 0
  %36 = insertvalue { i8*, i32 } %35, i32 %34, 1
  resume { i8*, i32 } %36
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) i32** @_ZNSt3__117__compressed_pairIPjNS_9allocatorIjEEE5firstEv(%"class.std::__1::__compressed_pair"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::__compressed_pair"*, align 8
  store %"class.std::__1::__compressed_pair"* %0, %"class.std::__1::__compressed_pair"** %2, align 8
  %3 = load %"class.std::__1::__compressed_pair"*, %"class.std::__1::__compressed_pair"** %2, align 8
  %4 = bitcast %"class.std::__1::__compressed_pair"* %3 to %"struct.std::__1::__compressed_pair_elem"*
  %5 = call nonnull align 8 dereferenceable(8) i32** @_ZNSt3__122__compressed_pair_elemIPjLi0ELb0EE5__getEv(%"struct.std::__1::__compressed_pair_elem"* %4) #11
  ret i32** %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) i32** @_ZNSt3__122__compressed_pair_elemIPjLi0ELb0EE5__getEv(%"struct.std::__1::__compressed_pair_elem"* %0) #1 align 2 {
  %2 = alloca %"struct.std::__1::__compressed_pair_elem"*, align 8
  store %"struct.std::__1::__compressed_pair_elem"* %0, %"struct.std::__1::__compressed_pair_elem"** %2, align 8
  %3 = load %"struct.std::__1::__compressed_pair_elem"*, %"struct.std::__1::__compressed_pair_elem"** %2, align 8
  %4 = getelementptr inbounds %"struct.std::__1::__compressed_pair_elem", %"struct.std::__1::__compressed_pair_elem"* %3, i32 0, i32 0
  ret i32** %4
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__16vectorIjNS_9allocatorIjEEE21_ConstructTransactionC1ERS3_m(%"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %0, %"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %1, i64 %2) unnamed_addr #0 align 2 {
  %4 = alloca %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"*, align 8
  %5 = alloca %"class.std::__1::vector"*, align 8
  %6 = alloca i64, align 8
  store %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %0, %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"** %4, align 8
  store %"class.std::__1::vector"* %1, %"class.std::__1::vector"** %5, align 8
  store i64 %2, i64* %6, align 8
  %7 = load %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"*, %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"** %4, align 8
  %8 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %5, align 8
  %9 = load i64, i64* %6, align 8
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEE21_ConstructTransactionC2ERS3_m(%"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %7, %"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %8, i64 %9)
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE9constructIjJRKjEEEvRS2_PT_DpOT0_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %0, i32* %1, i32* nonnull align 4 dereferenceable(4) %2) #0 align 2 {
  %4 = alloca %"class.std::__1::allocator"*, align 8
  %5 = alloca i32*, align 8
  %6 = alloca i32*, align 8
  %7 = alloca %"struct.std::__1::integral_constant", align 1
  %8 = alloca %"struct.std::__1::__has_construct", align 1
  store %"class.std::__1::allocator"* %0, %"class.std::__1::allocator"** %4, align 8
  store i32* %1, i32** %5, align 8
  store i32* %2, i32** %6, align 8
  %9 = bitcast %"struct.std::__1::__has_construct"* %8 to %"struct.std::__1::integral_constant"*
  %10 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %4, align 8
  %11 = load i32*, i32** %5, align 8
  %12 = load i32*, i32** %6, align 8
  %13 = call nonnull align 4 dereferenceable(4) i32* @_ZNSt3__17forwardIRKjEEOT_RNS_16remove_referenceIS3_E4typeE(i32* nonnull align 4 dereferenceable(4) %12) #11
  call void @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE11__constructIjJRKjEEEvNS_17integral_constantIbLb1EEERS2_PT_DpOT0_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %10, i32* %11, i32* nonnull align 4 dereferenceable(4) %13)
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 4 dereferenceable(4) i32* @_ZNSt3__17forwardIRKjEEOT_RNS_16remove_referenceIS3_E4typeE(i32* nonnull align 4 dereferenceable(4) %0) #1 {
  %2 = alloca i32*, align 8
  store i32* %0, i32** %2, align 8
  %3 = load i32*, i32** %2, align 8
  ret i32* %3
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZNSt3__16vectorIjNS_9allocatorIjEEE21_ConstructTransactionD1Ev(%"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %0) unnamed_addr #1 align 2 {
  %2 = alloca %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"*, align 8
  store %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %0, %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"** %2, align 8
  %3 = load %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"*, %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"** %2, align 8
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEE21_ConstructTransactionD2Ev(%"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %3) #11
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZNSt3__16vectorIjNS_9allocatorIjEEE21_ConstructTransactionC2ERS3_m(%"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %0, %"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %1, i64 %2) unnamed_addr #1 align 2 {
  %4 = alloca %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"*, align 8
  %5 = alloca %"class.std::__1::vector"*, align 8
  %6 = alloca i64, align 8
  store %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %0, %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"** %4, align 8
  store %"class.std::__1::vector"* %1, %"class.std::__1::vector"** %5, align 8
  store i64 %2, i64* %6, align 8
  %7 = load %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"*, %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"** %4, align 8
  %8 = getelementptr inbounds %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction", %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %7, i32 0, i32 0
  %9 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %5, align 8
  store %"class.std::__1::vector"* %9, %"class.std::__1::vector"** %8, align 8
  %10 = getelementptr inbounds %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction", %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %7, i32 0, i32 1
  %11 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %5, align 8
  %12 = bitcast %"class.std::__1::vector"* %11 to %"class.std::__1::__vector_base"*
  %13 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %12, i32 0, i32 1
  %14 = load i32*, i32** %13, align 8
  store i32* %14, i32** %10, align 8
  %15 = getelementptr inbounds %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction", %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %7, i32 0, i32 2
  %16 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %5, align 8
  %17 = bitcast %"class.std::__1::vector"* %16 to %"class.std::__1::__vector_base"*
  %18 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %17, i32 0, i32 1
  %19 = load i32*, i32** %18, align 8
  %20 = load i64, i64* %6, align 8
  %21 = getelementptr inbounds i32, i32* %19, i64 %20
  store i32* %21, i32** %15, align 8
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE11__constructIjJRKjEEEvNS_17integral_constantIbLb1EEERS2_PT_DpOT0_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %0, i32* %1, i32* nonnull align 4 dereferenceable(4) %2) #0 align 2 {
  %4 = alloca %"struct.std::__1::integral_constant", align 1
  %5 = alloca %"class.std::__1::allocator"*, align 8
  %6 = alloca i32*, align 8
  %7 = alloca i32*, align 8
  store %"class.std::__1::allocator"* %0, %"class.std::__1::allocator"** %5, align 8
  store i32* %1, i32** %6, align 8
  store i32* %2, i32** %7, align 8
  %8 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %5, align 8
  %9 = load i32*, i32** %6, align 8
  %10 = load i32*, i32** %7, align 8
  %11 = call nonnull align 4 dereferenceable(4) i32* @_ZNSt3__17forwardIRKjEEOT_RNS_16remove_referenceIS3_E4typeE(i32* nonnull align 4 dereferenceable(4) %10) #11
  call void @_ZNSt3__19allocatorIjE9constructIjJRKjEEEvPT_DpOT0_(%"class.std::__1::allocator"* %8, i32* %9, i32* nonnull align 4 dereferenceable(4) %11)
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZNSt3__19allocatorIjE9constructIjJRKjEEEvPT_DpOT0_(%"class.std::__1::allocator"* %0, i32* %1, i32* nonnull align 4 dereferenceable(4) %2) #1 align 2 {
  %4 = alloca %"class.std::__1::allocator"*, align 8
  %5 = alloca i32*, align 8
  %6 = alloca i32*, align 8
  store %"class.std::__1::allocator"* %0, %"class.std::__1::allocator"** %4, align 8
  store i32* %1, i32** %5, align 8
  store i32* %2, i32** %6, align 8
  %7 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %4, align 8
  %8 = load i32*, i32** %5, align 8
  %9 = bitcast i32* %8 to i8*
  %10 = bitcast i8* %9 to i32*
  %11 = load i32*, i32** %6, align 8
  %12 = call nonnull align 4 dereferenceable(4) i32* @_ZNSt3__17forwardIRKjEEOT_RNS_16remove_referenceIS3_E4typeE(i32* nonnull align 4 dereferenceable(4) %11) #11
  %13 = load i32, i32* %12, align 4
  store i32 %13, i32* %10, align 4
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZNSt3__16vectorIjNS_9allocatorIjEEE21_ConstructTransactionD2Ev(%"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %0) unnamed_addr #1 align 2 {
  %2 = alloca %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"*, align 8
  store %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %0, %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"** %2, align 8
  %3 = load %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"*, %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"** %2, align 8
  %4 = getelementptr inbounds %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction", %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %3, i32 0, i32 1
  %5 = load i32*, i32** %4, align 8
  %6 = getelementptr inbounds %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction", %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %3, i32 0, i32 0
  %7 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %6, align 8
  %8 = bitcast %"class.std::__1::vector"* %7 to %"class.std::__1::__vector_base"*
  %9 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %8, i32 0, i32 1
  store i32* %5, i32** %9, align 8
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr hidden i64 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE11__recommendEm(%"class.std::__1::vector"* %0, i64 %1) #0 align 2 {
  %3 = alloca i64, align 8
  %4 = alloca %"class.std::__1::vector"*, align 8
  %5 = alloca i64, align 8
  %6 = alloca i64, align 8
  %7 = alloca i64, align 8
  %8 = alloca i64, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %4, align 8
  store i64 %1, i64* %5, align 8
  %9 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %4, align 8
  %10 = call i64 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE8max_sizeEv(%"class.std::__1::vector"* %9) #11
  store i64 %10, i64* %6, align 8
  %11 = load i64, i64* %5, align 8
  %12 = load i64, i64* %6, align 8
  %13 = icmp ugt i64 %11, %12
  br i1 %13, label %14, label %16

14:                                               ; preds = %2
  %15 = bitcast %"class.std::__1::vector"* %9 to %"class.std::__1::__vector_base_common"*
  call void @_ZNKSt3__120__vector_base_commonILb1EE20__throw_length_errorEv(%"class.std::__1::__vector_base_common"* %15) #18
  unreachable

16:                                               ; preds = %2
  %17 = call i64 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE8capacityEv(%"class.std::__1::vector"* %9) #11
  store i64 %17, i64* %7, align 8
  %18 = load i64, i64* %7, align 8
  %19 = load i64, i64* %6, align 8
  %20 = udiv i64 %19, 2
  %21 = icmp uge i64 %18, %20
  br i1 %21, label %22, label %24

22:                                               ; preds = %16
  %23 = load i64, i64* %6, align 8
  store i64 %23, i64* %3, align 8
  br label %29

24:                                               ; preds = %16
  %25 = load i64, i64* %7, align 8
  %26 = mul i64 2, %25
  store i64 %26, i64* %8, align 8
  %27 = call nonnull align 8 dereferenceable(8) i64* @_ZNSt3__13maxImEERKT_S3_S3_(i64* nonnull align 8 dereferenceable(8) %8, i64* nonnull align 8 dereferenceable(8) %5)
  %28 = load i64, i64* %27, align 8
  store i64 %28, i64* %3, align 8
  br label %29

29:                                               ; preds = %24, %22
  %30 = load i64, i64* %3, align 8
  ret i64 %30
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEEC1EmmS3_(%"struct.std::__1::__split_buffer"* %0, i64 %1, i64 %2, %"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %3) unnamed_addr #0 align 2 {
  %5 = alloca %"struct.std::__1::__split_buffer"*, align 8
  %6 = alloca i64, align 8
  %7 = alloca i64, align 8
  %8 = alloca %"class.std::__1::allocator"*, align 8
  store %"struct.std::__1::__split_buffer"* %0, %"struct.std::__1::__split_buffer"** %5, align 8
  store i64 %1, i64* %6, align 8
  store i64 %2, i64* %7, align 8
  store %"class.std::__1::allocator"* %3, %"class.std::__1::allocator"** %8, align 8
  %9 = load %"struct.std::__1::__split_buffer"*, %"struct.std::__1::__split_buffer"** %5, align 8
  %10 = load i64, i64* %6, align 8
  %11 = load i64, i64* %7, align 8
  %12 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %8, align 8
  call void @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEEC2EmmS3_(%"struct.std::__1::__split_buffer"* %9, i64 %10, i64 %11, %"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %12)
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__16vectorIjNS_9allocatorIjEEE26__swap_out_circular_bufferERNS_14__split_bufferIjRS2_EE(%"class.std::__1::vector"* %0, %"struct.std::__1::__split_buffer"* nonnull align 8 dereferenceable(40) %1) #0 align 2 {
  %3 = alloca %"class.std::__1::vector"*, align 8
  %4 = alloca %"struct.std::__1::__split_buffer"*, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %3, align 8
  store %"struct.std::__1::__split_buffer"* %1, %"struct.std::__1::__split_buffer"** %4, align 8
  %5 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  call void @_ZNKSt3__16vectorIjNS_9allocatorIjEEE17__annotate_deleteEv(%"class.std::__1::vector"* %5) #11
  %6 = bitcast %"class.std::__1::vector"* %5 to %"class.std::__1::__vector_base"*
  %7 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEE7__allocEv(%"class.std::__1::__vector_base"* %6) #11
  %8 = bitcast %"class.std::__1::vector"* %5 to %"class.std::__1::__vector_base"*
  %9 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %8, i32 0, i32 0
  %10 = load i32*, i32** %9, align 8
  %11 = bitcast %"class.std::__1::vector"* %5 to %"class.std::__1::__vector_base"*
  %12 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %11, i32 0, i32 1
  %13 = load i32*, i32** %12, align 8
  %14 = load %"struct.std::__1::__split_buffer"*, %"struct.std::__1::__split_buffer"** %4, align 8
  %15 = getelementptr inbounds %"struct.std::__1::__split_buffer", %"struct.std::__1::__split_buffer"* %14, i32 0, i32 1
  call void @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE46__construct_backward_with_exception_guaranteesIjEENS_9enable_ifIXaaooL_ZNS_17integral_constantIbLb1EE5valueEEntsr15__has_constructIS2_PT_S8_EE5valuesr31is_trivially_move_constructibleIS8_EE5valueEvE4typeERS2_S9_S9_RS9_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %7, i32* %10, i32* %13, i32** nonnull align 8 dereferenceable(8) %15)
  %16 = bitcast %"class.std::__1::vector"* %5 to %"class.std::__1::__vector_base"*
  %17 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %16, i32 0, i32 0
  %18 = load %"struct.std::__1::__split_buffer"*, %"struct.std::__1::__split_buffer"** %4, align 8
  %19 = getelementptr inbounds %"struct.std::__1::__split_buffer", %"struct.std::__1::__split_buffer"* %18, i32 0, i32 1
  call void @_ZNSt3__14swapIPjEENS_9enable_ifIXaasr21is_move_constructibleIT_EE5valuesr18is_move_assignableIS3_EE5valueEvE4typeERS3_S6_(i32** nonnull align 8 dereferenceable(8) %17, i32** nonnull align 8 dereferenceable(8) %19) #11
  %20 = bitcast %"class.std::__1::vector"* %5 to %"class.std::__1::__vector_base"*
  %21 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %20, i32 0, i32 1
  %22 = load %"struct.std::__1::__split_buffer"*, %"struct.std::__1::__split_buffer"** %4, align 8
  %23 = getelementptr inbounds %"struct.std::__1::__split_buffer", %"struct.std::__1::__split_buffer"* %22, i32 0, i32 2
  call void @_ZNSt3__14swapIPjEENS_9enable_ifIXaasr21is_move_constructibleIT_EE5valuesr18is_move_assignableIS3_EE5valueEvE4typeERS3_S6_(i32** nonnull align 8 dereferenceable(8) %21, i32** nonnull align 8 dereferenceable(8) %23) #11
  %24 = bitcast %"class.std::__1::vector"* %5 to %"class.std::__1::__vector_base"*
  %25 = call nonnull align 8 dereferenceable(8) i32** @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEE9__end_capEv(%"class.std::__1::__vector_base"* %24) #11
  %26 = load %"struct.std::__1::__split_buffer"*, %"struct.std::__1::__split_buffer"** %4, align 8
  %27 = call nonnull align 8 dereferenceable(8) i32** @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEE9__end_capEv(%"struct.std::__1::__split_buffer"* %26) #11
  call void @_ZNSt3__14swapIPjEENS_9enable_ifIXaasr21is_move_constructibleIT_EE5valuesr18is_move_assignableIS3_EE5valueEvE4typeERS3_S6_(i32** nonnull align 8 dereferenceable(8) %25, i32** nonnull align 8 dereferenceable(8) %27) #11
  %28 = load %"struct.std::__1::__split_buffer"*, %"struct.std::__1::__split_buffer"** %4, align 8
  %29 = getelementptr inbounds %"struct.std::__1::__split_buffer", %"struct.std::__1::__split_buffer"* %28, i32 0, i32 1
  %30 = load i32*, i32** %29, align 8
  %31 = load %"struct.std::__1::__split_buffer"*, %"struct.std::__1::__split_buffer"** %4, align 8
  %32 = getelementptr inbounds %"struct.std::__1::__split_buffer", %"struct.std::__1::__split_buffer"* %31, i32 0, i32 0
  store i32* %30, i32** %32, align 8
  %33 = call i64 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE4sizeEv(%"class.std::__1::vector"* %5) #11
  call void @_ZNKSt3__16vectorIjNS_9allocatorIjEEE14__annotate_newEm(%"class.std::__1::vector"* %5, i64 %33) #11
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEE26__invalidate_all_iteratorsEv(%"class.std::__1::vector"* %5)
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEED1Ev(%"struct.std::__1::__split_buffer"* %0) unnamed_addr #1 align 2 {
  %2 = alloca %"struct.std::__1::__split_buffer"*, align 8
  store %"struct.std::__1::__split_buffer"* %0, %"struct.std::__1::__split_buffer"** %2, align 8
  %3 = load %"struct.std::__1::__split_buffer"*, %"struct.std::__1::__split_buffer"** %2, align 8
  call void @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEED2Ev(%"struct.std::__1::__split_buffer"* %3) #11
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr i64 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE8max_sizeEv(%"class.std::__1::vector"* %0) #1 align 2 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %2 = alloca %"class.std::__1::vector"*, align 8
  %3 = alloca i64, align 8
  %4 = alloca i64, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %2, align 8
  %5 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %2, align 8
  %6 = bitcast %"class.std::__1::vector"* %5 to %"class.std::__1::__vector_base"*
  %7 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNKSt3__113__vector_baseIjNS_9allocatorIjEEE7__allocEv(%"class.std::__1::__vector_base"* %6) #11
  %8 = call i64 @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE8max_sizeERKS2_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %7) #11
  store i64 %8, i64* %3, align 8
  %9 = call i64 @_ZNSt3__114numeric_limitsIlE3maxEv() #11
  store i64 %9, i64* %4, align 8
  %10 = invoke nonnull align 8 dereferenceable(8) i64* @_ZNSt3__13minImEERKT_S3_S3_(i64* nonnull align 8 dereferenceable(8) %3, i64* nonnull align 8 dereferenceable(8) %4)
          to label %11 unwind label %13

11:                                               ; preds = %1
  %12 = load i64, i64* %10, align 8
  ret i64 %12

13:                                               ; preds = %1
  %14 = landingpad { i8*, i32 }
          catch i8* null
  %15 = extractvalue { i8*, i32 } %14, 0
  call void @__clang_call_terminate(i8* %15) #17
  unreachable
}

; Function Attrs: noreturn
declare void @_ZNKSt3__120__vector_base_commonILb1EE20__throw_length_errorEv(%"class.std::__1::__vector_base_common"*) #10

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) i64* @_ZNSt3__13maxImEERKT_S3_S3_(i64* nonnull align 8 dereferenceable(8) %0, i64* nonnull align 8 dereferenceable(8) %1) #0 {
  %3 = alloca i64*, align 8
  %4 = alloca i64*, align 8
  %5 = alloca %"struct.std::__1::__less", align 1
  store i64* %0, i64** %3, align 8
  store i64* %1, i64** %4, align 8
  %6 = load i64*, i64** %3, align 8
  %7 = load i64*, i64** %4, align 8
  %8 = call nonnull align 8 dereferenceable(8) i64* @_ZNSt3__13maxImNS_6__lessImmEEEERKT_S5_S5_T0_(i64* nonnull align 8 dereferenceable(8) %6, i64* nonnull align 8 dereferenceable(8) %7)
  ret i64* %8
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) i64* @_ZNSt3__13minImEERKT_S3_S3_(i64* nonnull align 8 dereferenceable(8) %0, i64* nonnull align 8 dereferenceable(8) %1) #0 {
  %3 = alloca i64*, align 8
  %4 = alloca i64*, align 8
  %5 = alloca %"struct.std::__1::__less", align 1
  store i64* %0, i64** %3, align 8
  store i64* %1, i64** %4, align 8
  %6 = load i64*, i64** %3, align 8
  %7 = load i64*, i64** %4, align 8
  %8 = call nonnull align 8 dereferenceable(8) i64* @_ZNSt3__13minImNS_6__lessImmEEEERKT_S5_S5_T0_(i64* nonnull align 8 dereferenceable(8) %6, i64* nonnull align 8 dereferenceable(8) %7)
  ret i64* %8
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i64 @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE8max_sizeERKS2_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %0) #1 align 2 {
  %2 = alloca %"class.std::__1::allocator"*, align 8
  %3 = alloca %"struct.std::__1::integral_constant", align 1
  %4 = alloca %"struct.std::__1::__has_max_size", align 1
  store %"class.std::__1::allocator"* %0, %"class.std::__1::allocator"** %2, align 8
  %5 = bitcast %"struct.std::__1::__has_max_size"* %4 to %"struct.std::__1::integral_constant"*
  %6 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %2, align 8
  %7 = call i64 @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE10__max_sizeENS_17integral_constantIbLb1EEERKS2_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %6) #11
  ret i64 %7
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNKSt3__113__vector_baseIjNS_9allocatorIjEEE7__allocEv(%"class.std::__1::__vector_base"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::__vector_base"*, align 8
  store %"class.std::__1::__vector_base"* %0, %"class.std::__1::__vector_base"** %2, align 8
  %3 = load %"class.std::__1::__vector_base"*, %"class.std::__1::__vector_base"** %2, align 8
  %4 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %3, i32 0, i32 2
  %5 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNKSt3__117__compressed_pairIPjNS_9allocatorIjEEE6secondEv(%"class.std::__1::__compressed_pair"* %4) #11
  ret %"class.std::__1::allocator"* %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i64 @_ZNSt3__114numeric_limitsIlE3maxEv() #1 align 2 {
  %1 = call i64 @_ZNSt3__123__libcpp_numeric_limitsIlLb1EE3maxEv() #11
  ret i64 %1
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) i64* @_ZNSt3__13minImNS_6__lessImmEEEERKT_S5_S5_T0_(i64* nonnull align 8 dereferenceable(8) %0, i64* nonnull align 8 dereferenceable(8) %1) #0 {
  %3 = alloca %"struct.std::__1::__less", align 1
  %4 = alloca i64*, align 8
  %5 = alloca i64*, align 8
  store i64* %0, i64** %4, align 8
  store i64* %1, i64** %5, align 8
  %6 = load i64*, i64** %5, align 8
  %7 = load i64*, i64** %4, align 8
  %8 = call zeroext i1 @_ZNKSt3__16__lessImmEclERKmS3_(%"struct.std::__1::__less"* %3, i64* nonnull align 8 dereferenceable(8) %6, i64* nonnull align 8 dereferenceable(8) %7)
  br i1 %8, label %9, label %11

9:                                                ; preds = %2
  %10 = load i64*, i64** %5, align 8
  br label %13

11:                                               ; preds = %2
  %12 = load i64*, i64** %4, align 8
  br label %13

13:                                               ; preds = %11, %9
  %14 = phi i64* [ %10, %9 ], [ %12, %11 ]
  ret i64* %14
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden zeroext i1 @_ZNKSt3__16__lessImmEclERKmS3_(%"struct.std::__1::__less"* %0, i64* nonnull align 8 dereferenceable(8) %1, i64* nonnull align 8 dereferenceable(8) %2) #1 align 2 {
  %4 = alloca %"struct.std::__1::__less"*, align 8
  %5 = alloca i64*, align 8
  %6 = alloca i64*, align 8
  store %"struct.std::__1::__less"* %0, %"struct.std::__1::__less"** %4, align 8
  store i64* %1, i64** %5, align 8
  store i64* %2, i64** %6, align 8
  %7 = load %"struct.std::__1::__less"*, %"struct.std::__1::__less"** %4, align 8
  %8 = load i64*, i64** %5, align 8
  %9 = load i64, i64* %8, align 8
  %10 = load i64*, i64** %6, align 8
  %11 = load i64, i64* %10, align 8
  %12 = icmp ult i64 %9, %11
  ret i1 %12
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i64 @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE10__max_sizeENS_17integral_constantIbLb1EEERKS2_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %0) #1 align 2 {
  %2 = alloca %"struct.std::__1::integral_constant", align 1
  %3 = alloca %"class.std::__1::allocator"*, align 8
  store %"class.std::__1::allocator"* %0, %"class.std::__1::allocator"** %3, align 8
  %4 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %3, align 8
  %5 = call i64 @_ZNKSt3__19allocatorIjE8max_sizeEv(%"class.std::__1::allocator"* %4) #11
  ret i64 %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i64 @_ZNKSt3__19allocatorIjE8max_sizeEv(%"class.std::__1::allocator"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::allocator"*, align 8
  store %"class.std::__1::allocator"* %0, %"class.std::__1::allocator"** %2, align 8
  %3 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %2, align 8
  ret i64 4611686018427387903
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNKSt3__117__compressed_pairIPjNS_9allocatorIjEEE6secondEv(%"class.std::__1::__compressed_pair"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::__compressed_pair"*, align 8
  store %"class.std::__1::__compressed_pair"* %0, %"class.std::__1::__compressed_pair"** %2, align 8
  %3 = load %"class.std::__1::__compressed_pair"*, %"class.std::__1::__compressed_pair"** %2, align 8
  %4 = bitcast %"class.std::__1::__compressed_pair"* %3 to %"struct.std::__1::__compressed_pair_elem.0"*
  %5 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNKSt3__122__compressed_pair_elemINS_9allocatorIjEELi1ELb1EE5__getEv(%"struct.std::__1::__compressed_pair_elem.0"* %4) #11
  ret %"class.std::__1::allocator"* %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNKSt3__122__compressed_pair_elemINS_9allocatorIjEELi1ELb1EE5__getEv(%"struct.std::__1::__compressed_pair_elem.0"* %0) #1 align 2 {
  %2 = alloca %"struct.std::__1::__compressed_pair_elem.0"*, align 8
  store %"struct.std::__1::__compressed_pair_elem.0"* %0, %"struct.std::__1::__compressed_pair_elem.0"** %2, align 8
  %3 = load %"struct.std::__1::__compressed_pair_elem.0"*, %"struct.std::__1::__compressed_pair_elem.0"** %2, align 8
  %4 = bitcast %"struct.std::__1::__compressed_pair_elem.0"* %3 to %"class.std::__1::allocator"*
  ret %"class.std::__1::allocator"* %4
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i64 @_ZNSt3__123__libcpp_numeric_limitsIlLb1EE3maxEv() #1 align 2 {
  ret i64 9223372036854775807
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) i64* @_ZNSt3__13maxImNS_6__lessImmEEEERKT_S5_S5_T0_(i64* nonnull align 8 dereferenceable(8) %0, i64* nonnull align 8 dereferenceable(8) %1) #1 {
  %3 = alloca %"struct.std::__1::__less", align 1
  %4 = alloca i64*, align 8
  %5 = alloca i64*, align 8
  store i64* %0, i64** %4, align 8
  store i64* %1, i64** %5, align 8
  %6 = load i64*, i64** %4, align 8
  %7 = load i64*, i64** %5, align 8
  %8 = call zeroext i1 @_ZNKSt3__16__lessImmEclERKmS3_(%"struct.std::__1::__less"* %3, i64* nonnull align 8 dereferenceable(8) %6, i64* nonnull align 8 dereferenceable(8) %7)
  br i1 %8, label %9, label %11

9:                                                ; preds = %2
  %10 = load i64*, i64** %5, align 8
  br label %13

11:                                               ; preds = %2
  %12 = load i64*, i64** %4, align 8
  br label %13

13:                                               ; preds = %11, %9
  %14 = phi i64* [ %10, %9 ], [ %12, %11 ]
  ret i64* %14
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEEC2EmmS3_(%"struct.std::__1::__split_buffer"* %0, i64 %1, i64 %2, %"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %3) unnamed_addr #0 align 2 {
  %5 = alloca %"struct.std::__1::__split_buffer"*, align 8
  %6 = alloca i64, align 8
  %7 = alloca i64, align 8
  %8 = alloca %"class.std::__1::allocator"*, align 8
  %9 = alloca i8*, align 8
  store %"struct.std::__1::__split_buffer"* %0, %"struct.std::__1::__split_buffer"** %5, align 8
  store i64 %1, i64* %6, align 8
  store i64 %2, i64* %7, align 8
  store %"class.std::__1::allocator"* %3, %"class.std::__1::allocator"** %8, align 8
  %10 = load %"struct.std::__1::__split_buffer"*, %"struct.std::__1::__split_buffer"** %5, align 8
  %11 = bitcast %"struct.std::__1::__split_buffer"* %10 to %"class.std::__1::__split_buffer_common"*
  %12 = getelementptr inbounds %"struct.std::__1::__split_buffer", %"struct.std::__1::__split_buffer"* %10, i32 0, i32 3
  store i8* null, i8** %9, align 8
  %13 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %8, align 8
  call void @_ZNSt3__117__compressed_pairIPjRNS_9allocatorIjEEEC1IDnS4_EEOT_OT0_(%"class.std::__1::__compressed_pair.17"* %12, i8** nonnull align 8 dereferenceable(8) %9, %"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %13)
  %14 = load i64, i64* %6, align 8
  %15 = icmp ne i64 %14, 0
  br i1 %15, label %16, label %20

16:                                               ; preds = %4
  %17 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEE7__allocEv(%"struct.std::__1::__split_buffer"* %10) #11
  %18 = load i64, i64* %6, align 8
  %19 = call i32* @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE8allocateERS2_m(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %17, i64 %18)
  br label %21

20:                                               ; preds = %4
  br label %21

21:                                               ; preds = %20, %16
  %22 = phi i32* [ %19, %16 ], [ null, %20 ]
  %23 = getelementptr inbounds %"struct.std::__1::__split_buffer", %"struct.std::__1::__split_buffer"* %10, i32 0, i32 0
  store i32* %22, i32** %23, align 8
  %24 = getelementptr inbounds %"struct.std::__1::__split_buffer", %"struct.std::__1::__split_buffer"* %10, i32 0, i32 0
  %25 = load i32*, i32** %24, align 8
  %26 = load i64, i64* %7, align 8
  %27 = getelementptr inbounds i32, i32* %25, i64 %26
  %28 = getelementptr inbounds %"struct.std::__1::__split_buffer", %"struct.std::__1::__split_buffer"* %10, i32 0, i32 2
  store i32* %27, i32** %28, align 8
  %29 = getelementptr inbounds %"struct.std::__1::__split_buffer", %"struct.std::__1::__split_buffer"* %10, i32 0, i32 1
  store i32* %27, i32** %29, align 8
  %30 = getelementptr inbounds %"struct.std::__1::__split_buffer", %"struct.std::__1::__split_buffer"* %10, i32 0, i32 0
  %31 = load i32*, i32** %30, align 8
  %32 = load i64, i64* %6, align 8
  %33 = getelementptr inbounds i32, i32* %31, i64 %32
  %34 = call nonnull align 8 dereferenceable(8) i32** @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEE9__end_capEv(%"struct.std::__1::__split_buffer"* %10) #11
  store i32* %33, i32** %34, align 8
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__117__compressed_pairIPjRNS_9allocatorIjEEEC1IDnS4_EEOT_OT0_(%"class.std::__1::__compressed_pair.17"* %0, i8** nonnull align 8 dereferenceable(8) %1, %"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %2) unnamed_addr #0 align 2 {
  %4 = alloca %"class.std::__1::__compressed_pair.17"*, align 8
  %5 = alloca i8**, align 8
  %6 = alloca %"class.std::__1::allocator"*, align 8
  store %"class.std::__1::__compressed_pair.17"* %0, %"class.std::__1::__compressed_pair.17"** %4, align 8
  store i8** %1, i8*** %5, align 8
  store %"class.std::__1::allocator"* %2, %"class.std::__1::allocator"** %6, align 8
  %7 = load %"class.std::__1::__compressed_pair.17"*, %"class.std::__1::__compressed_pair.17"** %4, align 8
  %8 = load i8**, i8*** %5, align 8
  %9 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %6, align 8
  call void @_ZNSt3__117__compressed_pairIPjRNS_9allocatorIjEEEC2IDnS4_EEOT_OT0_(%"class.std::__1::__compressed_pair.17"* %7, i8** nonnull align 8 dereferenceable(8) %8, %"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %9)
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr hidden i32* @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE8allocateERS2_m(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %0, i64 %1) #0 align 2 {
  %3 = alloca %"class.std::__1::allocator"*, align 8
  %4 = alloca i64, align 8
  store %"class.std::__1::allocator"* %0, %"class.std::__1::allocator"** %3, align 8
  store i64 %1, i64* %4, align 8
  %5 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %3, align 8
  %6 = load i64, i64* %4, align 8
  %7 = call i32* @_ZNSt3__19allocatorIjE8allocateEm(%"class.std::__1::allocator"* %5, i64 %6)
  ret i32* %7
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEE7__allocEv(%"struct.std::__1::__split_buffer"* %0) #1 align 2 {
  %2 = alloca %"struct.std::__1::__split_buffer"*, align 8
  store %"struct.std::__1::__split_buffer"* %0, %"struct.std::__1::__split_buffer"** %2, align 8
  %3 = load %"struct.std::__1::__split_buffer"*, %"struct.std::__1::__split_buffer"** %2, align 8
  %4 = getelementptr inbounds %"struct.std::__1::__split_buffer", %"struct.std::__1::__split_buffer"* %3, i32 0, i32 3
  %5 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__117__compressed_pairIPjRNS_9allocatorIjEEE6secondEv(%"class.std::__1::__compressed_pair.17"* %4) #11
  ret %"class.std::__1::allocator"* %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) i32** @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEE9__end_capEv(%"struct.std::__1::__split_buffer"* %0) #1 align 2 {
  %2 = alloca %"struct.std::__1::__split_buffer"*, align 8
  store %"struct.std::__1::__split_buffer"* %0, %"struct.std::__1::__split_buffer"** %2, align 8
  %3 = load %"struct.std::__1::__split_buffer"*, %"struct.std::__1::__split_buffer"** %2, align 8
  %4 = getelementptr inbounds %"struct.std::__1::__split_buffer", %"struct.std::__1::__split_buffer"* %3, i32 0, i32 3
  %5 = call nonnull align 8 dereferenceable(8) i32** @_ZNSt3__117__compressed_pairIPjRNS_9allocatorIjEEE5firstEv(%"class.std::__1::__compressed_pair.17"* %4) #11
  ret i32** %5
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__117__compressed_pairIPjRNS_9allocatorIjEEEC2IDnS4_EEOT_OT0_(%"class.std::__1::__compressed_pair.17"* %0, i8** nonnull align 8 dereferenceable(8) %1, %"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %2) unnamed_addr #0 align 2 {
  %4 = alloca %"class.std::__1::__compressed_pair.17"*, align 8
  %5 = alloca i8**, align 8
  %6 = alloca %"class.std::__1::allocator"*, align 8
  store %"class.std::__1::__compressed_pair.17"* %0, %"class.std::__1::__compressed_pair.17"** %4, align 8
  store i8** %1, i8*** %5, align 8
  store %"class.std::__1::allocator"* %2, %"class.std::__1::allocator"** %6, align 8
  %7 = load %"class.std::__1::__compressed_pair.17"*, %"class.std::__1::__compressed_pair.17"** %4, align 8
  %8 = bitcast %"class.std::__1::__compressed_pair.17"* %7 to %"struct.std::__1::__compressed_pair_elem"*
  %9 = load i8**, i8*** %5, align 8
  %10 = call nonnull align 8 dereferenceable(8) i8** @_ZNSt3__17forwardIDnEEOT_RNS_16remove_referenceIS1_E4typeE(i8** nonnull align 8 dereferenceable(8) %9) #11
  call void @_ZNSt3__122__compressed_pair_elemIPjLi0ELb0EEC2IDnvEEOT_(%"struct.std::__1::__compressed_pair_elem"* %8, i8** nonnull align 8 dereferenceable(8) %10)
  %11 = bitcast %"class.std::__1::__compressed_pair.17"* %7 to i8*
  %12 = getelementptr inbounds i8, i8* %11, i64 8
  %13 = bitcast i8* %12 to %"struct.std::__1::__compressed_pair_elem.18"*
  %14 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %6, align 8
  %15 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__17forwardIRNS_9allocatorIjEEEEOT_RNS_16remove_referenceIS4_E4typeE(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %14) #11
  call void @_ZNSt3__122__compressed_pair_elemIRNS_9allocatorIjEELi1ELb0EEC2IS3_vEEOT_(%"struct.std::__1::__compressed_pair_elem.18"* %13, %"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %15)
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__17forwardIRNS_9allocatorIjEEEEOT_RNS_16remove_referenceIS4_E4typeE(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %0) #1 {
  %2 = alloca %"class.std::__1::allocator"*, align 8
  store %"class.std::__1::allocator"* %0, %"class.std::__1::allocator"** %2, align 8
  %3 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %2, align 8
  ret %"class.std::__1::allocator"* %3
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZNSt3__122__compressed_pair_elemIRNS_9allocatorIjEELi1ELb0EEC2IS3_vEEOT_(%"struct.std::__1::__compressed_pair_elem.18"* %0, %"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %1) unnamed_addr #1 align 2 {
  %3 = alloca %"struct.std::__1::__compressed_pair_elem.18"*, align 8
  %4 = alloca %"class.std::__1::allocator"*, align 8
  store %"struct.std::__1::__compressed_pair_elem.18"* %0, %"struct.std::__1::__compressed_pair_elem.18"** %3, align 8
  store %"class.std::__1::allocator"* %1, %"class.std::__1::allocator"** %4, align 8
  %5 = load %"struct.std::__1::__compressed_pair_elem.18"*, %"struct.std::__1::__compressed_pair_elem.18"** %3, align 8
  %6 = getelementptr inbounds %"struct.std::__1::__compressed_pair_elem.18", %"struct.std::__1::__compressed_pair_elem.18"* %5, i32 0, i32 0
  %7 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %4, align 8
  %8 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__17forwardIRNS_9allocatorIjEEEEOT_RNS_16remove_referenceIS4_E4typeE(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %7) #11
  store %"class.std::__1::allocator"* %8, %"class.std::__1::allocator"** %6, align 8
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr hidden i32* @_ZNSt3__19allocatorIjE8allocateEm(%"class.std::__1::allocator"* %0, i64 %1) #0 align 2 {
  %3 = alloca %"class.std::__1::allocator"*, align 8
  %4 = alloca i64, align 8
  store %"class.std::__1::allocator"* %0, %"class.std::__1::allocator"** %3, align 8
  store i64 %1, i64* %4, align 8
  %5 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %3, align 8
  %6 = load i64, i64* %4, align 8
  %7 = icmp ugt i64 %6, 4611686018427387903
  br i1 %7, label %8, label %9

8:                                                ; preds = %2
  call void @_ZNSt3__120__throw_length_errorEPKc(i8* getelementptr inbounds ([68 x i8], [68 x i8]* @.str.43, i64 0, i64 0)) #18
  unreachable

9:                                                ; preds = %2
  %10 = load i64, i64* %4, align 8
  %11 = mul i64 %10, 4
  %12 = call i8* @_ZNSt3__117__libcpp_allocateEmm(i64 %11, i64 4)
  %13 = bitcast i8* %12 to i32*
  ret i32* %13
}

; Function Attrs: noinline noreturn optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__120__throw_length_errorEPKc(i8* %0) #13 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %2 = alloca i8*, align 8
  %3 = alloca i8*, align 8
  %4 = alloca i32, align 4
  store i8* %0, i8** %2, align 8
  %5 = call i8* @__cxa_allocate_exception(i64 16) #11
  %6 = bitcast i8* %5 to %"class.std::length_error"*
  %7 = load i8*, i8** %2, align 8
  invoke void @_ZNSt12length_errorC1EPKc(%"class.std::length_error"* %6, i8* %7)
          to label %8 unwind label %9

8:                                                ; preds = %1
  call void @__cxa_throw(i8* %5, i8* bitcast (i8** @_ZTISt12length_error to i8*), i8* bitcast (void (%"class.std::length_error"*)* @_ZNSt12length_errorD1Ev to i8*)) #18
  unreachable

9:                                                ; preds = %1
  %10 = landingpad { i8*, i32 }
          cleanup
  %11 = extractvalue { i8*, i32 } %10, 0
  store i8* %11, i8** %3, align 8
  %12 = extractvalue { i8*, i32 } %10, 1
  store i32 %12, i32* %4, align 4
  call void @__cxa_free_exception(i8* %5) #11
  br label %13

13:                                               ; preds = %9
  %14 = load i8*, i8** %3, align 8
  %15 = load i32, i32* %4, align 4
  %16 = insertvalue { i8*, i32 } undef, i8* %14, 0
  %17 = insertvalue { i8*, i32 } %16, i32 %15, 1
  resume { i8*, i32 } %17
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr hidden i8* @_ZNSt3__117__libcpp_allocateEmm(i64 %0, i64 %1) #0 {
  %3 = alloca i64, align 8
  %4 = alloca i64, align 8
  store i64 %0, i64* %3, align 8
  store i64 %1, i64* %4, align 8
  %5 = load i64, i64* %3, align 8
  %6 = call noalias nonnull i8* @_Znwm(i64 %5) #16
  ret i8* %6
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt12length_errorC1EPKc(%"class.std::length_error"* %0, i8* %1) unnamed_addr #0 align 2 {
  %3 = alloca %"class.std::length_error"*, align 8
  %4 = alloca i8*, align 8
  store %"class.std::length_error"* %0, %"class.std::length_error"** %3, align 8
  store i8* %1, i8** %4, align 8
  %5 = load %"class.std::length_error"*, %"class.std::length_error"** %3, align 8
  %6 = load i8*, i8** %4, align 8
  call void @_ZNSt12length_errorC2EPKc(%"class.std::length_error"* %5, i8* %6)
  ret void
}

; Function Attrs: nounwind
declare void @_ZNSt12length_errorD1Ev(%"class.std::length_error"*) unnamed_addr #12

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt12length_errorC2EPKc(%"class.std::length_error"* %0, i8* %1) unnamed_addr #0 align 2 {
  %3 = alloca %"class.std::length_error"*, align 8
  %4 = alloca i8*, align 8
  store %"class.std::length_error"* %0, %"class.std::length_error"** %3, align 8
  store i8* %1, i8** %4, align 8
  %5 = load %"class.std::length_error"*, %"class.std::length_error"** %3, align 8
  %6 = bitcast %"class.std::length_error"* %5 to %"class.std::logic_error"*
  %7 = load i8*, i8** %4, align 8
  call void @_ZNSt11logic_errorC2EPKc(%"class.std::logic_error"* %6, i8* %7)
  %8 = bitcast %"class.std::length_error"* %5 to i32 (...)***
  store i32 (...)** bitcast (i8** getelementptr inbounds ({ [5 x i8*] }, { [5 x i8*] }* @_ZTVSt12length_error, i32 0, inrange i32 0, i32 2) to i32 (...)**), i32 (...)*** %8, align 8
  ret void
}

declare void @_ZNSt11logic_errorC2EPKc(%"class.std::logic_error"*, i8*) unnamed_addr #2

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__117__compressed_pairIPjRNS_9allocatorIjEEE6secondEv(%"class.std::__1::__compressed_pair.17"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::__compressed_pair.17"*, align 8
  store %"class.std::__1::__compressed_pair.17"* %0, %"class.std::__1::__compressed_pair.17"** %2, align 8
  %3 = load %"class.std::__1::__compressed_pair.17"*, %"class.std::__1::__compressed_pair.17"** %2, align 8
  %4 = bitcast %"class.std::__1::__compressed_pair.17"* %3 to i8*
  %5 = getelementptr inbounds i8, i8* %4, i64 8
  %6 = bitcast i8* %5 to %"struct.std::__1::__compressed_pair_elem.18"*
  %7 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__122__compressed_pair_elemIRNS_9allocatorIjEELi1ELb0EE5__getEv(%"struct.std::__1::__compressed_pair_elem.18"* %6) #11
  ret %"class.std::__1::allocator"* %7
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__122__compressed_pair_elemIRNS_9allocatorIjEELi1ELb0EE5__getEv(%"struct.std::__1::__compressed_pair_elem.18"* %0) #1 align 2 {
  %2 = alloca %"struct.std::__1::__compressed_pair_elem.18"*, align 8
  store %"struct.std::__1::__compressed_pair_elem.18"* %0, %"struct.std::__1::__compressed_pair_elem.18"** %2, align 8
  %3 = load %"struct.std::__1::__compressed_pair_elem.18"*, %"struct.std::__1::__compressed_pair_elem.18"** %2, align 8
  %4 = getelementptr inbounds %"struct.std::__1::__compressed_pair_elem.18", %"struct.std::__1::__compressed_pair_elem.18"* %3, i32 0, i32 0
  %5 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %4, align 8
  ret %"class.std::__1::allocator"* %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) i32** @_ZNSt3__117__compressed_pairIPjRNS_9allocatorIjEEE5firstEv(%"class.std::__1::__compressed_pair.17"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::__compressed_pair.17"*, align 8
  store %"class.std::__1::__compressed_pair.17"* %0, %"class.std::__1::__compressed_pair.17"** %2, align 8
  %3 = load %"class.std::__1::__compressed_pair.17"*, %"class.std::__1::__compressed_pair.17"** %2, align 8
  %4 = bitcast %"class.std::__1::__compressed_pair.17"* %3 to %"struct.std::__1::__compressed_pair_elem"*
  %5 = call nonnull align 8 dereferenceable(8) i32** @_ZNSt3__122__compressed_pair_elemIPjLi0ELb0EE5__getEv(%"struct.std::__1::__compressed_pair_elem"* %4) #11
  ret i32** %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE46__construct_backward_with_exception_guaranteesIjEENS_9enable_ifIXaaooL_ZNS_17integral_constantIbLb1EE5valueEEntsr15__has_constructIS2_PT_S8_EE5valuesr31is_trivially_move_constructibleIS8_EE5valueEvE4typeERS2_S9_S9_RS9_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %0, i32* %1, i32* %2, i32** nonnull align 8 dereferenceable(8) %3) #1 align 2 {
  %5 = alloca %"class.std::__1::allocator"*, align 8
  %6 = alloca i32*, align 8
  %7 = alloca i32*, align 8
  %8 = alloca i32**, align 8
  %9 = alloca i64, align 8
  store %"class.std::__1::allocator"* %0, %"class.std::__1::allocator"** %5, align 8
  store i32* %1, i32** %6, align 8
  store i32* %2, i32** %7, align 8
  store i32** %3, i32*** %8, align 8
  %10 = load i32*, i32** %7, align 8
  %11 = load i32*, i32** %6, align 8
  %12 = ptrtoint i32* %10 to i64
  %13 = ptrtoint i32* %11 to i64
  %14 = sub i64 %12, %13
  %15 = sdiv exact i64 %14, 4
  store i64 %15, i64* %9, align 8
  %16 = load i64, i64* %9, align 8
  %17 = load i32**, i32*** %8, align 8
  %18 = load i32*, i32** %17, align 8
  %19 = sub i64 0, %16
  %20 = getelementptr inbounds i32, i32* %18, i64 %19
  store i32* %20, i32** %17, align 8
  %21 = load i64, i64* %9, align 8
  %22 = icmp sgt i64 %21, 0
  br i1 %22, label %23, label %31

23:                                               ; preds = %4
  %24 = load i32**, i32*** %8, align 8
  %25 = load i32*, i32** %24, align 8
  %26 = bitcast i32* %25 to i8*
  %27 = load i32*, i32** %6, align 8
  %28 = bitcast i32* %27 to i8*
  %29 = load i64, i64* %9, align 8
  %30 = mul i64 %29, 4
  call void @llvm.memcpy.p0i8.p0i8.i64(i8* align 4 %26, i8* align 4 %28, i64 %30, i1 false)
  br label %31

31:                                               ; preds = %23, %4
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__14swapIPjEENS_9enable_ifIXaasr21is_move_constructibleIT_EE5valuesr18is_move_assignableIS3_EE5valueEvE4typeERS3_S6_(i32** nonnull align 8 dereferenceable(8) %0, i32** nonnull align 8 dereferenceable(8) %1) #1 {
  %3 = alloca i32**, align 8
  %4 = alloca i32**, align 8
  %5 = alloca i32*, align 8
  store i32** %0, i32*** %3, align 8
  store i32** %1, i32*** %4, align 8
  %6 = load i32**, i32*** %3, align 8
  %7 = call nonnull align 8 dereferenceable(8) i32** @_ZNSt3__14moveIRPjEEONS_16remove_referenceIT_E4typeEOS4_(i32** nonnull align 8 dereferenceable(8) %6) #11
  %8 = load i32*, i32** %7, align 8
  store i32* %8, i32** %5, align 8
  %9 = load i32**, i32*** %4, align 8
  %10 = call nonnull align 8 dereferenceable(8) i32** @_ZNSt3__14moveIRPjEEONS_16remove_referenceIT_E4typeEOS4_(i32** nonnull align 8 dereferenceable(8) %9) #11
  %11 = load i32*, i32** %10, align 8
  %12 = load i32**, i32*** %3, align 8
  store i32* %11, i32** %12, align 8
  %13 = call nonnull align 8 dereferenceable(8) i32** @_ZNSt3__14moveIRPjEEONS_16remove_referenceIT_E4typeEOS4_(i32** nonnull align 8 dereferenceable(8) %5) #11
  %14 = load i32*, i32** %13, align 8
  %15 = load i32**, i32*** %4, align 8
  store i32* %14, i32** %15, align 8
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNKSt3__16vectorIjNS_9allocatorIjEEE14__annotate_newEm(%"class.std::__1::vector"* %0, i64 %1) #1 align 2 {
  %3 = alloca %"class.std::__1::vector"*, align 8
  %4 = alloca i64, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %3, align 8
  store i64 %1, i64* %4, align 8
  %5 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  %6 = call i32* @_ZNKSt3__16vectorIjNS_9allocatorIjEEE4dataEv(%"class.std::__1::vector"* %5) #11
  %7 = bitcast i32* %6 to i8*
  %8 = call i32* @_ZNKSt3__16vectorIjNS_9allocatorIjEEE4dataEv(%"class.std::__1::vector"* %5) #11
  %9 = call i64 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE8capacityEv(%"class.std::__1::vector"* %5) #11
  %10 = getelementptr inbounds i32, i32* %8, i64 %9
  %11 = bitcast i32* %10 to i8*
  %12 = call i32* @_ZNKSt3__16vectorIjNS_9allocatorIjEEE4dataEv(%"class.std::__1::vector"* %5) #11
  %13 = call i64 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE8capacityEv(%"class.std::__1::vector"* %5) #11
  %14 = getelementptr inbounds i32, i32* %12, i64 %13
  %15 = bitcast i32* %14 to i8*
  %16 = call i32* @_ZNKSt3__16vectorIjNS_9allocatorIjEEE4dataEv(%"class.std::__1::vector"* %5) #11
  %17 = load i64, i64* %4, align 8
  %18 = getelementptr inbounds i32, i32* %16, i64 %17
  %19 = bitcast i32* %18 to i8*
  call void @_ZNKSt3__16vectorIjNS_9allocatorIjEEE31__annotate_contiguous_containerEPKvS5_S5_S5_(%"class.std::__1::vector"* %5, i8* %7, i8* %11, i8* %15, i8* %19) #11
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__16vectorIjNS_9allocatorIjEEE26__invalidate_all_iteratorsEv(%"class.std::__1::vector"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::vector"*, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %2, align 8
  %3 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %2, align 8
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) i32** @_ZNSt3__14moveIRPjEEONS_16remove_referenceIT_E4typeEOS4_(i32** nonnull align 8 dereferenceable(8) %0) #1 {
  %2 = alloca i32**, align 8
  store i32** %0, i32*** %2, align 8
  %3 = load i32**, i32*** %2, align 8
  ret i32** %3
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEED2Ev(%"struct.std::__1::__split_buffer"* %0) unnamed_addr #1 align 2 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %2 = alloca %"struct.std::__1::__split_buffer"*, align 8
  store %"struct.std::__1::__split_buffer"* %0, %"struct.std::__1::__split_buffer"** %2, align 8
  %3 = load %"struct.std::__1::__split_buffer"*, %"struct.std::__1::__split_buffer"** %2, align 8
  call void @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEE5clearEv(%"struct.std::__1::__split_buffer"* %3) #11
  %4 = getelementptr inbounds %"struct.std::__1::__split_buffer", %"struct.std::__1::__split_buffer"* %3, i32 0, i32 0
  %5 = load i32*, i32** %4, align 8
  %6 = icmp ne i32* %5, null
  br i1 %6, label %7, label %13

7:                                                ; preds = %1
  %8 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEE7__allocEv(%"struct.std::__1::__split_buffer"* %3) #11
  %9 = getelementptr inbounds %"struct.std::__1::__split_buffer", %"struct.std::__1::__split_buffer"* %3, i32 0, i32 0
  %10 = load i32*, i32** %9, align 8
  %11 = invoke i64 @_ZNKSt3__114__split_bufferIjRNS_9allocatorIjEEE8capacityEv(%"struct.std::__1::__split_buffer"* %3)
          to label %12 unwind label %14

12:                                               ; preds = %7
  call void @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE10deallocateERS2_Pjm(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %8, i32* %10, i64 %11) #11
  br label %13

13:                                               ; preds = %12, %1
  ret void

14:                                               ; preds = %7
  %15 = landingpad { i8*, i32 }
          catch i8* null
  %16 = extractvalue { i8*, i32 } %15, 0
  call void @__clang_call_terminate(i8* %16) #17
  unreachable
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEE5clearEv(%"struct.std::__1::__split_buffer"* %0) #1 align 2 {
  %2 = alloca %"struct.std::__1::__split_buffer"*, align 8
  store %"struct.std::__1::__split_buffer"* %0, %"struct.std::__1::__split_buffer"** %2, align 8
  %3 = load %"struct.std::__1::__split_buffer"*, %"struct.std::__1::__split_buffer"** %2, align 8
  %4 = getelementptr inbounds %"struct.std::__1::__split_buffer", %"struct.std::__1::__split_buffer"* %3, i32 0, i32 1
  %5 = load i32*, i32** %4, align 8
  call void @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEE17__destruct_at_endEPj(%"struct.std::__1::__split_buffer"* %3, i32* %5) #11
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i64 @_ZNKSt3__114__split_bufferIjRNS_9allocatorIjEEE8capacityEv(%"struct.std::__1::__split_buffer"* %0) #1 align 2 {
  %2 = alloca %"struct.std::__1::__split_buffer"*, align 8
  store %"struct.std::__1::__split_buffer"* %0, %"struct.std::__1::__split_buffer"** %2, align 8
  %3 = load %"struct.std::__1::__split_buffer"*, %"struct.std::__1::__split_buffer"** %2, align 8
  %4 = call nonnull align 8 dereferenceable(8) i32** @_ZNKSt3__114__split_bufferIjRNS_9allocatorIjEEE9__end_capEv(%"struct.std::__1::__split_buffer"* %3) #11
  %5 = load i32*, i32** %4, align 8
  %6 = getelementptr inbounds %"struct.std::__1::__split_buffer", %"struct.std::__1::__split_buffer"* %3, i32 0, i32 0
  %7 = load i32*, i32** %6, align 8
  %8 = ptrtoint i32* %5 to i64
  %9 = ptrtoint i32* %7 to i64
  %10 = sub i64 %8, %9
  %11 = sdiv exact i64 %10, 4
  ret i64 %11
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEE17__destruct_at_endEPj(%"struct.std::__1::__split_buffer"* %0, i32* %1) #1 align 2 {
  %3 = alloca %"struct.std::__1::__split_buffer"*, align 8
  %4 = alloca i32*, align 8
  %5 = alloca %"struct.std::__1::integral_constant.19", align 1
  store %"struct.std::__1::__split_buffer"* %0, %"struct.std::__1::__split_buffer"** %3, align 8
  store i32* %1, i32** %4, align 8
  %6 = load %"struct.std::__1::__split_buffer"*, %"struct.std::__1::__split_buffer"** %3, align 8
  %7 = load i32*, i32** %4, align 8
  call void @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEE17__destruct_at_endEPjNS_17integral_constantIbLb0EEE(%"struct.std::__1::__split_buffer"* %6, i32* %7) #11
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEE17__destruct_at_endEPjNS_17integral_constantIbLb0EEE(%"struct.std::__1::__split_buffer"* %0, i32* %1) #1 align 2 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %3 = alloca %"struct.std::__1::integral_constant.19", align 1
  %4 = alloca %"struct.std::__1::__split_buffer"*, align 8
  %5 = alloca i32*, align 8
  store %"struct.std::__1::__split_buffer"* %0, %"struct.std::__1::__split_buffer"** %4, align 8
  store i32* %1, i32** %5, align 8
  %6 = load %"struct.std::__1::__split_buffer"*, %"struct.std::__1::__split_buffer"** %4, align 8
  br label %7

7:                                                ; preds = %18, %2
  %8 = load i32*, i32** %5, align 8
  %9 = getelementptr inbounds %"struct.std::__1::__split_buffer", %"struct.std::__1::__split_buffer"* %6, i32 0, i32 2
  %10 = load i32*, i32** %9, align 8
  %11 = icmp ne i32* %8, %10
  br i1 %11, label %12, label %19

12:                                               ; preds = %7
  %13 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEE7__allocEv(%"struct.std::__1::__split_buffer"* %6) #11
  %14 = getelementptr inbounds %"struct.std::__1::__split_buffer", %"struct.std::__1::__split_buffer"* %6, i32 0, i32 2
  %15 = load i32*, i32** %14, align 8
  %16 = getelementptr inbounds i32, i32* %15, i32 -1
  store i32* %16, i32** %14, align 8
  %17 = call i32* @_ZNSt3__112__to_addressIjEEPT_S2_(i32* %16) #11
  invoke void @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE7destroyIjEEvRS2_PT_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %13, i32* %17)
          to label %18 unwind label %20

18:                                               ; preds = %12
  br label %7

19:                                               ; preds = %7
  ret void

20:                                               ; preds = %12
  %21 = landingpad { i8*, i32 }
          catch i8* null
  %22 = extractvalue { i8*, i32 } %21, 0
  call void @__clang_call_terminate(i8* %22) #17
  unreachable
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) i32** @_ZNKSt3__114__split_bufferIjRNS_9allocatorIjEEE9__end_capEv(%"struct.std::__1::__split_buffer"* %0) #1 align 2 {
  %2 = alloca %"struct.std::__1::__split_buffer"*, align 8
  store %"struct.std::__1::__split_buffer"* %0, %"struct.std::__1::__split_buffer"** %2, align 8
  %3 = load %"struct.std::__1::__split_buffer"*, %"struct.std::__1::__split_buffer"** %2, align 8
  %4 = getelementptr inbounds %"struct.std::__1::__split_buffer", %"struct.std::__1::__split_buffer"* %3, i32 0, i32 3
  %5 = call nonnull align 8 dereferenceable(8) i32** @_ZNKSt3__117__compressed_pairIPjRNS_9allocatorIjEEE5firstEv(%"class.std::__1::__compressed_pair.17"* %4) #11
  ret i32** %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) i32** @_ZNKSt3__117__compressed_pairIPjRNS_9allocatorIjEEE5firstEv(%"class.std::__1::__compressed_pair.17"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::__compressed_pair.17"*, align 8
  store %"class.std::__1::__compressed_pair.17"* %0, %"class.std::__1::__compressed_pair.17"** %2, align 8
  %3 = load %"class.std::__1::__compressed_pair.17"*, %"class.std::__1::__compressed_pair.17"** %2, align 8
  %4 = bitcast %"class.std::__1::__compressed_pair.17"* %3 to %"struct.std::__1::__compressed_pair_elem"*
  %5 = call nonnull align 8 dereferenceable(8) i32** @_ZNKSt3__122__compressed_pair_elemIPjLi0ELb0EE5__getEv(%"struct.std::__1::__compressed_pair_elem"* %4) #11
  ret i32** %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__16vectorIjNS_9allocatorIjEEEC2EOS3_(%"class.std::__1::vector"* %0, %"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %1) unnamed_addr #1 align 2 {
  %3 = alloca %"class.std::__1::vector"*, align 8
  %4 = alloca %"class.std::__1::vector"*, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %3, align 8
  store %"class.std::__1::vector"* %1, %"class.std::__1::vector"** %4, align 8
  %5 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  %6 = bitcast %"class.std::__1::vector"* %5 to %"class.std::__1::__vector_base"*
  %7 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %4, align 8
  %8 = bitcast %"class.std::__1::vector"* %7 to %"class.std::__1::__vector_base"*
  %9 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEE7__allocEv(%"class.std::__1::__vector_base"* %8) #11
  %10 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__14moveIRNS_9allocatorIjEEEEONS_16remove_referenceIT_E4typeEOS5_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %9) #11
  call void @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEEC2EOS2_(%"class.std::__1::__vector_base"* %6, %"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %10) #11
  %11 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %4, align 8
  %12 = bitcast %"class.std::__1::vector"* %11 to %"class.std::__1::__vector_base"*
  %13 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %12, i32 0, i32 0
  %14 = load i32*, i32** %13, align 8
  %15 = bitcast %"class.std::__1::vector"* %5 to %"class.std::__1::__vector_base"*
  %16 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %15, i32 0, i32 0
  store i32* %14, i32** %16, align 8
  %17 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %4, align 8
  %18 = bitcast %"class.std::__1::vector"* %17 to %"class.std::__1::__vector_base"*
  %19 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %18, i32 0, i32 1
  %20 = load i32*, i32** %19, align 8
  %21 = bitcast %"class.std::__1::vector"* %5 to %"class.std::__1::__vector_base"*
  %22 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %21, i32 0, i32 1
  store i32* %20, i32** %22, align 8
  %23 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %4, align 8
  %24 = bitcast %"class.std::__1::vector"* %23 to %"class.std::__1::__vector_base"*
  %25 = call nonnull align 8 dereferenceable(8) i32** @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEE9__end_capEv(%"class.std::__1::__vector_base"* %24) #11
  %26 = load i32*, i32** %25, align 8
  %27 = bitcast %"class.std::__1::vector"* %5 to %"class.std::__1::__vector_base"*
  %28 = call nonnull align 8 dereferenceable(8) i32** @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEE9__end_capEv(%"class.std::__1::__vector_base"* %27) #11
  store i32* %26, i32** %28, align 8
  %29 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %4, align 8
  %30 = bitcast %"class.std::__1::vector"* %29 to %"class.std::__1::__vector_base"*
  %31 = call nonnull align 8 dereferenceable(8) i32** @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEE9__end_capEv(%"class.std::__1::__vector_base"* %30) #11
  store i32* null, i32** %31, align 8
  %32 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %4, align 8
  %33 = bitcast %"class.std::__1::vector"* %32 to %"class.std::__1::__vector_base"*
  %34 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %33, i32 0, i32 1
  store i32* null, i32** %34, align 8
  %35 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %4, align 8
  %36 = bitcast %"class.std::__1::vector"* %35 to %"class.std::__1::__vector_base"*
  %37 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %36, i32 0, i32 0
  store i32* null, i32** %37, align 8
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__14moveIRNS_9allocatorIjEEEEONS_16remove_referenceIT_E4typeEOS5_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %0) #1 {
  %2 = alloca %"class.std::__1::allocator"*, align 8
  store %"class.std::__1::allocator"* %0, %"class.std::__1::allocator"** %2, align 8
  %3 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %2, align 8
  ret %"class.std::__1::allocator"* %3
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEEC2EOS2_(%"class.std::__1::__vector_base"* %0, %"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %1) unnamed_addr #1 align 2 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %3 = alloca %"class.std::__1::__vector_base"*, align 8
  %4 = alloca %"class.std::__1::allocator"*, align 8
  %5 = alloca i8*, align 8
  store %"class.std::__1::__vector_base"* %0, %"class.std::__1::__vector_base"** %3, align 8
  store %"class.std::__1::allocator"* %1, %"class.std::__1::allocator"** %4, align 8
  %6 = load %"class.std::__1::__vector_base"*, %"class.std::__1::__vector_base"** %3, align 8
  %7 = bitcast %"class.std::__1::__vector_base"* %6 to %"class.std::__1::__vector_base_common"*
  invoke void @_ZNSt3__120__vector_base_commonILb1EEC2Ev(%"class.std::__1::__vector_base_common"* %7)
          to label %8 unwind label %15

8:                                                ; preds = %2
  %9 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %6, i32 0, i32 0
  store i32* null, i32** %9, align 8
  %10 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %6, i32 0, i32 1
  store i32* null, i32** %10, align 8
  %11 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %6, i32 0, i32 2
  store i8* null, i8** %5, align 8
  %12 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %4, align 8
  %13 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__14moveIRNS_9allocatorIjEEEEONS_16remove_referenceIT_E4typeEOS5_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %12) #11
  invoke void @_ZNSt3__117__compressed_pairIPjNS_9allocatorIjEEEC1IDnS3_EEOT_OT0_(%"class.std::__1::__compressed_pair"* %11, i8** nonnull align 8 dereferenceable(8) %5, %"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %13)
          to label %14 unwind label %15

14:                                               ; preds = %8
  ret void

15:                                               ; preds = %8, %2
  %16 = landingpad { i8*, i32 }
          catch i8* null
  %17 = extractvalue { i8*, i32 } %16, 0
  call void @__clang_call_terminate(i8* %17) #17
  unreachable
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__117__compressed_pairIPjNS_9allocatorIjEEEC1IDnS3_EEOT_OT0_(%"class.std::__1::__compressed_pair"* %0, i8** nonnull align 8 dereferenceable(8) %1, %"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %2) unnamed_addr #0 align 2 {
  %4 = alloca %"class.std::__1::__compressed_pair"*, align 8
  %5 = alloca i8**, align 8
  %6 = alloca %"class.std::__1::allocator"*, align 8
  store %"class.std::__1::__compressed_pair"* %0, %"class.std::__1::__compressed_pair"** %4, align 8
  store i8** %1, i8*** %5, align 8
  store %"class.std::__1::allocator"* %2, %"class.std::__1::allocator"** %6, align 8
  %7 = load %"class.std::__1::__compressed_pair"*, %"class.std::__1::__compressed_pair"** %4, align 8
  %8 = load i8**, i8*** %5, align 8
  %9 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %6, align 8
  call void @_ZNSt3__117__compressed_pairIPjNS_9allocatorIjEEEC2IDnS3_EEOT_OT0_(%"class.std::__1::__compressed_pair"* %7, i8** nonnull align 8 dereferenceable(8) %8, %"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %9)
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__117__compressed_pairIPjNS_9allocatorIjEEEC2IDnS3_EEOT_OT0_(%"class.std::__1::__compressed_pair"* %0, i8** nonnull align 8 dereferenceable(8) %1, %"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %2) unnamed_addr #0 align 2 {
  %4 = alloca %"class.std::__1::__compressed_pair"*, align 8
  %5 = alloca i8**, align 8
  %6 = alloca %"class.std::__1::allocator"*, align 8
  store %"class.std::__1::__compressed_pair"* %0, %"class.std::__1::__compressed_pair"** %4, align 8
  store i8** %1, i8*** %5, align 8
  store %"class.std::__1::allocator"* %2, %"class.std::__1::allocator"** %6, align 8
  %7 = load %"class.std::__1::__compressed_pair"*, %"class.std::__1::__compressed_pair"** %4, align 8
  %8 = bitcast %"class.std::__1::__compressed_pair"* %7 to %"struct.std::__1::__compressed_pair_elem"*
  %9 = load i8**, i8*** %5, align 8
  %10 = call nonnull align 8 dereferenceable(8) i8** @_ZNSt3__17forwardIDnEEOT_RNS_16remove_referenceIS1_E4typeE(i8** nonnull align 8 dereferenceable(8) %9) #11
  call void @_ZNSt3__122__compressed_pair_elemIPjLi0ELb0EEC2IDnvEEOT_(%"struct.std::__1::__compressed_pair_elem"* %8, i8** nonnull align 8 dereferenceable(8) %10)
  %11 = bitcast %"class.std::__1::__compressed_pair"* %7 to %"struct.std::__1::__compressed_pair_elem.0"*
  %12 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %6, align 8
  %13 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__17forwardINS_9allocatorIjEEEEOT_RNS_16remove_referenceIS3_E4typeE(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %12) #11
  call void @_ZNSt3__122__compressed_pair_elemINS_9allocatorIjEELi1ELb1EEC2IS2_vEEOT_(%"struct.std::__1::__compressed_pair_elem.0"* %11, %"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %13)
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__17forwardINS_9allocatorIjEEEEOT_RNS_16remove_referenceIS3_E4typeE(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %0) #1 {
  %2 = alloca %"class.std::__1::allocator"*, align 8
  store %"class.std::__1::allocator"* %0, %"class.std::__1::allocator"** %2, align 8
  %3 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %2, align 8
  ret %"class.std::__1::allocator"* %3
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZNSt3__122__compressed_pair_elemINS_9allocatorIjEELi1ELb1EEC2IS2_vEEOT_(%"struct.std::__1::__compressed_pair_elem.0"* %0, %"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %1) unnamed_addr #1 align 2 {
  %3 = alloca %"struct.std::__1::__compressed_pair_elem.0"*, align 8
  %4 = alloca %"class.std::__1::allocator"*, align 8
  store %"struct.std::__1::__compressed_pair_elem.0"* %0, %"struct.std::__1::__compressed_pair_elem.0"** %3, align 8
  store %"class.std::__1::allocator"* %1, %"class.std::__1::allocator"** %4, align 8
  %5 = load %"struct.std::__1::__compressed_pair_elem.0"*, %"struct.std::__1::__compressed_pair_elem.0"** %3, align 8
  %6 = bitcast %"struct.std::__1::__compressed_pair_elem.0"* %5 to %"class.std::__1::allocator"*
  %7 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %4, align 8
  %8 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__17forwardINS_9allocatorIjEEEEOT_RNS_16remove_referenceIS3_E4typeE(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %7) #11
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__16vectorIjNS_9allocatorIjEEEC2ERKS3_(%"class.std::__1::vector"* %0, %"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %1) unnamed_addr #0 align 2 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %3 = alloca %"class.std::__1::vector"*, align 8
  %4 = alloca %"class.std::__1::vector"*, align 8
  %5 = alloca %"class.std::__1::allocator", align 1
  %6 = alloca %"class.std::__1::allocator", align 1
  %7 = alloca i64, align 8
  %8 = alloca i8*, align 8
  %9 = alloca i32, align 4
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %3, align 8
  store %"class.std::__1::vector"* %1, %"class.std::__1::vector"** %4, align 8
  %10 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  %11 = bitcast %"class.std::__1::vector"* %10 to %"class.std::__1::__vector_base"*
  %12 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %4, align 8
  %13 = bitcast %"class.std::__1::vector"* %12 to %"class.std::__1::__vector_base"*
  %14 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNKSt3__113__vector_baseIjNS_9allocatorIjEEE7__allocEv(%"class.std::__1::__vector_base"* %13) #11
  call void @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE37select_on_container_copy_constructionERKS2_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %14)
  call void @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEEC2EOS2_(%"class.std::__1::__vector_base"* %11, %"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %5) #11
  %15 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %4, align 8
  %16 = call i64 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE4sizeEv(%"class.std::__1::vector"* %15) #11
  store i64 %16, i64* %7, align 8
  %17 = load i64, i64* %7, align 8
  %18 = icmp ugt i64 %17, 0
  br i1 %18, label %19, label %37

19:                                               ; preds = %2
  %20 = load i64, i64* %7, align 8
  invoke void @_ZNSt3__16vectorIjNS_9allocatorIjEEE11__vallocateEm(%"class.std::__1::vector"* %10, i64 %20)
          to label %21 unwind label %32

21:                                               ; preds = %19
  %22 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %4, align 8
  %23 = bitcast %"class.std::__1::vector"* %22 to %"class.std::__1::__vector_base"*
  %24 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %23, i32 0, i32 0
  %25 = load i32*, i32** %24, align 8
  %26 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %4, align 8
  %27 = bitcast %"class.std::__1::vector"* %26 to %"class.std::__1::__vector_base"*
  %28 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %27, i32 0, i32 1
  %29 = load i32*, i32** %28, align 8
  %30 = load i64, i64* %7, align 8
  invoke void @_ZNSt3__16vectorIjNS_9allocatorIjEEE18__construct_at_endIPjEENS_9enable_ifIXsr27__is_cpp17_forward_iteratorIT_EE5valueEvE4typeES7_S7_m(%"class.std::__1::vector"* %10, i32* %25, i32* %29, i64 %30)
          to label %31 unwind label %32

31:                                               ; preds = %21
  br label %37

32:                                               ; preds = %21, %19
  %33 = landingpad { i8*, i32 }
          cleanup
  %34 = extractvalue { i8*, i32 } %33, 0
  store i8* %34, i8** %8, align 8
  %35 = extractvalue { i8*, i32 } %33, 1
  store i32 %35, i32* %9, align 4
  %36 = bitcast %"class.std::__1::vector"* %10 to %"class.std::__1::__vector_base"*
  call void @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEED2Ev(%"class.std::__1::__vector_base"* %36) #11
  br label %38

37:                                               ; preds = %31, %2
  ret void

38:                                               ; preds = %32
  %39 = load i8*, i8** %8, align 8
  %40 = load i32, i32* %9, align 4
  %41 = insertvalue { i8*, i32 } undef, i8* %39, 0
  %42 = insertvalue { i8*, i32 } %41, i32 %40, 1
  resume { i8*, i32 } %42
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE37select_on_container_copy_constructionERKS2_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %0) #0 align 2 {
  %2 = alloca %"class.std::__1::allocator"*, align 8
  %3 = alloca %"struct.std::__1::integral_constant.19", align 1
  %4 = alloca %"struct.std::__1::__has_select_on_container_copy_construction", align 1
  %5 = alloca %"class.std::__1::allocator", align 1
  store %"class.std::__1::allocator"* %0, %"class.std::__1::allocator"** %2, align 8
  %6 = bitcast %"struct.std::__1::__has_select_on_container_copy_construction"* %4 to %"struct.std::__1::integral_constant.19"*
  %7 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %2, align 8
  call void @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE39__select_on_container_copy_constructionENS_17integral_constantIbLb0EEERKS2_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %7)
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__16vectorIjNS_9allocatorIjEEE11__vallocateEm(%"class.std::__1::vector"* %0, i64 %1) #0 align 2 {
  %3 = alloca %"class.std::__1::vector"*, align 8
  %4 = alloca i64, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %3, align 8
  store i64 %1, i64* %4, align 8
  %5 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  %6 = load i64, i64* %4, align 8
  %7 = call i64 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE8max_sizeEv(%"class.std::__1::vector"* %5) #11
  %8 = icmp ugt i64 %6, %7
  br i1 %8, label %9, label %11

9:                                                ; preds = %2
  %10 = bitcast %"class.std::__1::vector"* %5 to %"class.std::__1::__vector_base_common"*
  call void @_ZNKSt3__120__vector_base_commonILb1EE20__throw_length_errorEv(%"class.std::__1::__vector_base_common"* %10) #18
  unreachable

11:                                               ; preds = %2
  %12 = bitcast %"class.std::__1::vector"* %5 to %"class.std::__1::__vector_base"*
  %13 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEE7__allocEv(%"class.std::__1::__vector_base"* %12) #11
  %14 = load i64, i64* %4, align 8
  %15 = call i32* @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE8allocateERS2_m(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %13, i64 %14)
  %16 = bitcast %"class.std::__1::vector"* %5 to %"class.std::__1::__vector_base"*
  %17 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %16, i32 0, i32 1
  store i32* %15, i32** %17, align 8
  %18 = bitcast %"class.std::__1::vector"* %5 to %"class.std::__1::__vector_base"*
  %19 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %18, i32 0, i32 0
  store i32* %15, i32** %19, align 8
  %20 = bitcast %"class.std::__1::vector"* %5 to %"class.std::__1::__vector_base"*
  %21 = getelementptr inbounds %"class.std::__1::__vector_base", %"class.std::__1::__vector_base"* %20, i32 0, i32 0
  %22 = load i32*, i32** %21, align 8
  %23 = load i64, i64* %4, align 8
  %24 = getelementptr inbounds i32, i32* %22, i64 %23
  %25 = bitcast %"class.std::__1::vector"* %5 to %"class.std::__1::__vector_base"*
  %26 = call nonnull align 8 dereferenceable(8) i32** @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEE9__end_capEv(%"class.std::__1::__vector_base"* %25) #11
  store i32* %24, i32** %26, align 8
  call void @_ZNKSt3__16vectorIjNS_9allocatorIjEEE14__annotate_newEm(%"class.std::__1::vector"* %5, i64 0) #11
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__16vectorIjNS_9allocatorIjEEE18__construct_at_endIPjEENS_9enable_ifIXsr27__is_cpp17_forward_iteratorIT_EE5valueEvE4typeES7_S7_m(%"class.std::__1::vector"* %0, i32* %1, i32* %2, i64 %3) #0 align 2 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %5 = alloca %"class.std::__1::vector"*, align 8
  %6 = alloca i32*, align 8
  %7 = alloca i32*, align 8
  %8 = alloca i64, align 8
  %9 = alloca %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction", align 8
  %10 = alloca i8*, align 8
  %11 = alloca i32, align 4
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %5, align 8
  store i32* %1, i32** %6, align 8
  store i32* %2, i32** %7, align 8
  store i64 %3, i64* %8, align 8
  %12 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %5, align 8
  %13 = load i64, i64* %8, align 8
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEE21_ConstructTransactionC1ERS3_m(%"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %9, %"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %12, i64 %13)
  %14 = bitcast %"class.std::__1::vector"* %12 to %"class.std::__1::__vector_base"*
  %15 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEE7__allocEv(%"class.std::__1::__vector_base"* %14) #11
  %16 = load i32*, i32** %6, align 8
  %17 = load i32*, i32** %7, align 8
  %18 = getelementptr inbounds %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction", %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %9, i32 0, i32 1
  invoke void @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE25__construct_range_forwardIjjjjEENS_9enable_ifIXaaaasr31is_trivially_copy_constructibleIT0_EE5valuesr7is_sameIT1_T2_EE5valueooL_ZNS_17integral_constantIbLb1EE5valueEEntsr15__has_constructIS2_PS6_RT_EE5valueEvE4typeERS2_PSC_SH_RSB_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %15, i32* %16, i32* %17, i32** nonnull align 8 dereferenceable(8) %18)
          to label %19 unwind label %20

19:                                               ; preds = %4
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEE21_ConstructTransactionD1Ev(%"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %9) #11
  ret void

20:                                               ; preds = %4
  %21 = landingpad { i8*, i32 }
          cleanup
  %22 = extractvalue { i8*, i32 } %21, 0
  store i8* %22, i8** %10, align 8
  %23 = extractvalue { i8*, i32 } %21, 1
  store i32 %23, i32* %11, align 4
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEE21_ConstructTransactionD1Ev(%"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %9) #11
  br label %24

24:                                               ; preds = %20
  %25 = load i8*, i8** %10, align 8
  %26 = load i32, i32* %11, align 4
  %27 = insertvalue { i8*, i32 } undef, i8* %25, 0
  %28 = insertvalue { i8*, i32 } %27, i32 %26, 1
  resume { i8*, i32 } %28
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE39__select_on_container_copy_constructionENS_17integral_constantIbLb0EEERKS2_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %0) #1 align 2 {
  %2 = alloca %"struct.std::__1::integral_constant.19", align 1
  %3 = alloca %"class.std::__1::allocator"*, align 8
  store %"class.std::__1::allocator"* %0, %"class.std::__1::allocator"** %3, align 8
  %4 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %3, align 8
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE25__construct_range_forwardIjjjjEENS_9enable_ifIXaaaasr31is_trivially_copy_constructibleIT0_EE5valuesr7is_sameIT1_T2_EE5valueooL_ZNS_17integral_constantIbLb1EE5valueEEntsr15__has_constructIS2_PS6_RT_EE5valueEvE4typeERS2_PSC_SH_RSB_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %0, i32* %1, i32* %2, i32** nonnull align 8 dereferenceable(8) %3) #1 align 2 {
  %5 = alloca %"class.std::__1::allocator"*, align 8
  %6 = alloca i32*, align 8
  %7 = alloca i32*, align 8
  %8 = alloca i32**, align 8
  %9 = alloca i64, align 8
  store %"class.std::__1::allocator"* %0, %"class.std::__1::allocator"** %5, align 8
  store i32* %1, i32** %6, align 8
  store i32* %2, i32** %7, align 8
  store i32** %3, i32*** %8, align 8
  %10 = load i32*, i32** %7, align 8
  %11 = load i32*, i32** %6, align 8
  %12 = ptrtoint i32* %10 to i64
  %13 = ptrtoint i32* %11 to i64
  %14 = sub i64 %12, %13
  %15 = sdiv exact i64 %14, 4
  store i64 %15, i64* %9, align 8
  %16 = load i64, i64* %9, align 8
  %17 = icmp sgt i64 %16, 0
  br i1 %17, label %18, label %30

18:                                               ; preds = %4
  %19 = load i32**, i32*** %8, align 8
  %20 = load i32*, i32** %19, align 8
  %21 = bitcast i32* %20 to i8*
  %22 = load i32*, i32** %6, align 8
  %23 = bitcast i32* %22 to i8*
  %24 = load i64, i64* %9, align 8
  %25 = mul i64 %24, 4
  call void @llvm.memcpy.p0i8.p0i8.i64(i8* align 4 %21, i8* align 4 %23, i64 %25, i1 false)
  %26 = load i64, i64* %9, align 8
  %27 = load i32**, i32*** %8, align 8
  %28 = load i32*, i32** %27, align 8
  %29 = getelementptr inbounds i32, i32* %28, i64 %26
  store i32* %29, i32** %27, align 8
  br label %30

30:                                               ; preds = %18, %4
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__16vectorIjNS_9allocatorIjEEE22__construct_one_at_endIJjEEEvDpOT_(%"class.std::__1::vector"* %0, i32* nonnull align 4 dereferenceable(4) %1) #0 align 2 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %3 = alloca %"class.std::__1::vector"*, align 8
  %4 = alloca i32*, align 8
  %5 = alloca %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction", align 8
  %6 = alloca i8*, align 8
  %7 = alloca i32, align 4
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %3, align 8
  store i32* %1, i32** %4, align 8
  %8 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEE21_ConstructTransactionC1ERS3_m(%"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %5, %"class.std::__1::vector"* nonnull align 8 dereferenceable(24) %8, i64 1)
  %9 = bitcast %"class.std::__1::vector"* %8 to %"class.std::__1::__vector_base"*
  %10 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEE7__allocEv(%"class.std::__1::__vector_base"* %9) #11
  %11 = getelementptr inbounds %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction", %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %5, i32 0, i32 1
  %12 = load i32*, i32** %11, align 8
  %13 = call i32* @_ZNSt3__112__to_addressIjEEPT_S2_(i32* %12) #11
  %14 = load i32*, i32** %4, align 8
  %15 = call nonnull align 4 dereferenceable(4) i32* @_ZNSt3__17forwardIjEEOT_RNS_16remove_referenceIS1_E4typeE(i32* nonnull align 4 dereferenceable(4) %14) #11
  invoke void @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE9constructIjJjEEEvRS2_PT_DpOT0_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %10, i32* %13, i32* nonnull align 4 dereferenceable(4) %15)
          to label %16 unwind label %20

16:                                               ; preds = %2
  %17 = getelementptr inbounds %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction", %"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %5, i32 0, i32 1
  %18 = load i32*, i32** %17, align 8
  %19 = getelementptr inbounds i32, i32* %18, i32 1
  store i32* %19, i32** %17, align 8
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEE21_ConstructTransactionD1Ev(%"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %5) #11
  ret void

20:                                               ; preds = %2
  %21 = landingpad { i8*, i32 }
          cleanup
  %22 = extractvalue { i8*, i32 } %21, 0
  store i8* %22, i8** %6, align 8
  %23 = extractvalue { i8*, i32 } %21, 1
  store i32 %23, i32* %7, align 4
  call void @_ZNSt3__16vectorIjNS_9allocatorIjEEE21_ConstructTransactionD1Ev(%"struct.std::__1::vector<unsigned int, std::__1::allocator<unsigned int>>::_ConstructTransaction"* %5) #11
  br label %24

24:                                               ; preds = %20
  %25 = load i8*, i8** %6, align 8
  %26 = load i32, i32* %7, align 4
  %27 = insertvalue { i8*, i32 } undef, i8* %25, 0
  %28 = insertvalue { i8*, i32 } %27, i32 %26, 1
  resume { i8*, i32 } %28
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 4 dereferenceable(4) i32* @_ZNSt3__14moveIRjEEONS_16remove_referenceIT_E4typeEOS3_(i32* nonnull align 4 dereferenceable(4) %0) #1 {
  %2 = alloca i32*, align 8
  store i32* %0, i32** %2, align 8
  %3 = load i32*, i32** %2, align 8
  ret i32* %3
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__16vectorIjNS_9allocatorIjEEE21__push_back_slow_pathIjEEvOT_(%"class.std::__1::vector"* %0, i32* nonnull align 4 dereferenceable(4) %1) #0 align 2 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %3 = alloca %"class.std::__1::vector"*, align 8
  %4 = alloca i32*, align 8
  %5 = alloca %"class.std::__1::allocator"*, align 8
  %6 = alloca %"struct.std::__1::__split_buffer", align 8
  %7 = alloca i8*, align 8
  %8 = alloca i32, align 4
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %3, align 8
  store i32* %1, i32** %4, align 8
  %9 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  %10 = bitcast %"class.std::__1::vector"* %9 to %"class.std::__1::__vector_base"*
  %11 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEE7__allocEv(%"class.std::__1::__vector_base"* %10) #11
  store %"class.std::__1::allocator"* %11, %"class.std::__1::allocator"** %5, align 8
  %12 = call i64 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE4sizeEv(%"class.std::__1::vector"* %9) #11
  %13 = add i64 %12, 1
  %14 = call i64 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE11__recommendEm(%"class.std::__1::vector"* %9, i64 %13)
  %15 = call i64 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE4sizeEv(%"class.std::__1::vector"* %9) #11
  %16 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %5, align 8
  call void @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEEC1EmmS3_(%"struct.std::__1::__split_buffer"* %6, i64 %14, i64 %15, %"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %16)
  %17 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %5, align 8
  %18 = getelementptr inbounds %"struct.std::__1::__split_buffer", %"struct.std::__1::__split_buffer"* %6, i32 0, i32 2
  %19 = load i32*, i32** %18, align 8
  %20 = call i32* @_ZNSt3__112__to_addressIjEEPT_S2_(i32* %19) #11
  %21 = load i32*, i32** %4, align 8
  %22 = call nonnull align 4 dereferenceable(4) i32* @_ZNSt3__17forwardIjEEOT_RNS_16remove_referenceIS1_E4typeE(i32* nonnull align 4 dereferenceable(4) %21) #11
  invoke void @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE9constructIjJjEEEvRS2_PT_DpOT0_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %17, i32* %20, i32* nonnull align 4 dereferenceable(4) %22)
          to label %23 unwind label %28

23:                                               ; preds = %2
  %24 = getelementptr inbounds %"struct.std::__1::__split_buffer", %"struct.std::__1::__split_buffer"* %6, i32 0, i32 2
  %25 = load i32*, i32** %24, align 8
  %26 = getelementptr inbounds i32, i32* %25, i32 1
  store i32* %26, i32** %24, align 8
  invoke void @_ZNSt3__16vectorIjNS_9allocatorIjEEE26__swap_out_circular_bufferERNS_14__split_bufferIjRS2_EE(%"class.std::__1::vector"* %9, %"struct.std::__1::__split_buffer"* nonnull align 8 dereferenceable(40) %6)
          to label %27 unwind label %28

27:                                               ; preds = %23
  call void @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEED1Ev(%"struct.std::__1::__split_buffer"* %6) #11
  ret void

28:                                               ; preds = %23, %2
  %29 = landingpad { i8*, i32 }
          cleanup
  %30 = extractvalue { i8*, i32 } %29, 0
  store i8* %30, i8** %7, align 8
  %31 = extractvalue { i8*, i32 } %29, 1
  store i32 %31, i32* %8, align 4
  call void @_ZNSt3__114__split_bufferIjRNS_9allocatorIjEEED1Ev(%"struct.std::__1::__split_buffer"* %6) #11
  br label %32

32:                                               ; preds = %28
  %33 = load i8*, i8** %7, align 8
  %34 = load i32, i32* %8, align 4
  %35 = insertvalue { i8*, i32 } undef, i8* %33, 0
  %36 = insertvalue { i8*, i32 } %35, i32 %34, 1
  resume { i8*, i32 } %36
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE9constructIjJjEEEvRS2_PT_DpOT0_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %0, i32* %1, i32* nonnull align 4 dereferenceable(4) %2) #0 align 2 {
  %4 = alloca %"class.std::__1::allocator"*, align 8
  %5 = alloca i32*, align 8
  %6 = alloca i32*, align 8
  %7 = alloca %"struct.std::__1::integral_constant", align 1
  %8 = alloca %"struct.std::__1::__has_construct.20", align 1
  store %"class.std::__1::allocator"* %0, %"class.std::__1::allocator"** %4, align 8
  store i32* %1, i32** %5, align 8
  store i32* %2, i32** %6, align 8
  %9 = bitcast %"struct.std::__1::__has_construct.20"* %8 to %"struct.std::__1::integral_constant"*
  %10 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %4, align 8
  %11 = load i32*, i32** %5, align 8
  %12 = load i32*, i32** %6, align 8
  %13 = call nonnull align 4 dereferenceable(4) i32* @_ZNSt3__17forwardIjEEOT_RNS_16remove_referenceIS1_E4typeE(i32* nonnull align 4 dereferenceable(4) %12) #11
  call void @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE11__constructIjJjEEEvNS_17integral_constantIbLb1EEERS2_PT_DpOT0_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %10, i32* %11, i32* nonnull align 4 dereferenceable(4) %13)
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 4 dereferenceable(4) i32* @_ZNSt3__17forwardIjEEOT_RNS_16remove_referenceIS1_E4typeE(i32* nonnull align 4 dereferenceable(4) %0) #1 {
  %2 = alloca i32*, align 8
  store i32* %0, i32** %2, align 8
  %3 = load i32*, i32** %2, align 8
  ret i32* %3
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__116allocator_traitsINS_9allocatorIjEEE11__constructIjJjEEEvNS_17integral_constantIbLb1EEERS2_PT_DpOT0_(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %0, i32* %1, i32* nonnull align 4 dereferenceable(4) %2) #0 align 2 {
  %4 = alloca %"struct.std::__1::integral_constant", align 1
  %5 = alloca %"class.std::__1::allocator"*, align 8
  %6 = alloca i32*, align 8
  %7 = alloca i32*, align 8
  store %"class.std::__1::allocator"* %0, %"class.std::__1::allocator"** %5, align 8
  store i32* %1, i32** %6, align 8
  store i32* %2, i32** %7, align 8
  %8 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %5, align 8
  %9 = load i32*, i32** %6, align 8
  %10 = load i32*, i32** %7, align 8
  %11 = call nonnull align 4 dereferenceable(4) i32* @_ZNSt3__17forwardIjEEOT_RNS_16remove_referenceIS1_E4typeE(i32* nonnull align 4 dereferenceable(4) %10) #11
  call void @_ZNSt3__19allocatorIjE9constructIjJjEEEvPT_DpOT0_(%"class.std::__1::allocator"* %8, i32* %9, i32* nonnull align 4 dereferenceable(4) %11)
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZNSt3__19allocatorIjE9constructIjJjEEEvPT_DpOT0_(%"class.std::__1::allocator"* %0, i32* %1, i32* nonnull align 4 dereferenceable(4) %2) #1 align 2 {
  %4 = alloca %"class.std::__1::allocator"*, align 8
  %5 = alloca i32*, align 8
  %6 = alloca i32*, align 8
  store %"class.std::__1::allocator"* %0, %"class.std::__1::allocator"** %4, align 8
  store i32* %1, i32** %5, align 8
  store i32* %2, i32** %6, align 8
  %7 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %4, align 8
  %8 = load i32*, i32** %5, align 8
  %9 = bitcast i32* %8 to i8*
  %10 = bitcast i8* %9 to i32*
  %11 = load i32*, i32** %6, align 8
  %12 = call nonnull align 4 dereferenceable(4) i32* @_ZNSt3__17forwardIjEEOT_RNS_16remove_referenceIS1_E4typeE(i32* nonnull align 4 dereferenceable(4) %11) #11
  %13 = load i32, i32* %12, align 4
  store i32 %13, i32* %10, align 4
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i32* @_ZNSt3__16vectorIjNS_9allocatorIjEEE11__make_iterEPj(%"class.std::__1::vector"* %0, i32* %1) #1 align 2 {
  %3 = alloca %"class.std::__1::__wrap_iter", align 8
  %4 = alloca %"class.std::__1::vector"*, align 8
  %5 = alloca i32*, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %4, align 8
  store i32* %1, i32** %5, align 8
  %6 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %4, align 8
  %7 = load i32*, i32** %5, align 8
  call void @_ZNSt3__111__wrap_iterIPjEC1ES1_(%"class.std::__1::__wrap_iter"* %3, i32* %7) #11
  %8 = getelementptr inbounds %"class.std::__1::__wrap_iter", %"class.std::__1::__wrap_iter"* %3, i32 0, i32 0
  %9 = load i32*, i32** %8, align 8
  ret i32* %9
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__111__wrap_iterIPjEC1ES1_(%"class.std::__1::__wrap_iter"* %0, i32* %1) unnamed_addr #1 align 2 {
  %3 = alloca %"class.std::__1::__wrap_iter"*, align 8
  %4 = alloca i32*, align 8
  store %"class.std::__1::__wrap_iter"* %0, %"class.std::__1::__wrap_iter"** %3, align 8
  store i32* %1, i32** %4, align 8
  %5 = load %"class.std::__1::__wrap_iter"*, %"class.std::__1::__wrap_iter"** %3, align 8
  %6 = load i32*, i32** %4, align 8
  call void @_ZNSt3__111__wrap_iterIPjEC2ES1_(%"class.std::__1::__wrap_iter"* %5, i32* %6) #11
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__111__wrap_iterIPjEC2ES1_(%"class.std::__1::__wrap_iter"* %0, i32* %1) unnamed_addr #1 align 2 {
  %3 = alloca %"class.std::__1::__wrap_iter"*, align 8
  %4 = alloca i32*, align 8
  store %"class.std::__1::__wrap_iter"* %0, %"class.std::__1::__wrap_iter"** %3, align 8
  store i32* %1, i32** %4, align 8
  %5 = load %"class.std::__1::__wrap_iter"*, %"class.std::__1::__wrap_iter"** %3, align 8
  %6 = getelementptr inbounds %"class.std::__1::__wrap_iter", %"class.std::__1::__wrap_iter"* %5, i32 0, i32 0
  %7 = load i32*, i32** %4, align 8
  store i32* %7, i32** %6, align 8
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden zeroext i1 @_ZNSt3__1neIPjEEbRKNS_11__wrap_iterIT_EES6_(%"class.std::__1::__wrap_iter"* nonnull align 8 dereferenceable(8) %0, %"class.std::__1::__wrap_iter"* nonnull align 8 dereferenceable(8) %1) #1 {
  %3 = alloca %"class.std::__1::__wrap_iter"*, align 8
  %4 = alloca %"class.std::__1::__wrap_iter"*, align 8
  store %"class.std::__1::__wrap_iter"* %0, %"class.std::__1::__wrap_iter"** %3, align 8
  store %"class.std::__1::__wrap_iter"* %1, %"class.std::__1::__wrap_iter"** %4, align 8
  %5 = load %"class.std::__1::__wrap_iter"*, %"class.std::__1::__wrap_iter"** %3, align 8
  %6 = load %"class.std::__1::__wrap_iter"*, %"class.std::__1::__wrap_iter"** %4, align 8
  %7 = call zeroext i1 @_ZNSt3__1eqIPjS1_EEbRKNS_11__wrap_iterIT_EERKNS2_IT0_EE(%"class.std::__1::__wrap_iter"* nonnull align 8 dereferenceable(8) %5, %"class.std::__1::__wrap_iter"* nonnull align 8 dereferenceable(8) %6) #11
  %8 = xor i1 %7, true
  ret i1 %8
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 4 dereferenceable(4) i32* @_ZNKSt3__111__wrap_iterIPjEdeEv(%"class.std::__1::__wrap_iter"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::__wrap_iter"*, align 8
  store %"class.std::__1::__wrap_iter"* %0, %"class.std::__1::__wrap_iter"** %2, align 8
  %3 = load %"class.std::__1::__wrap_iter"*, %"class.std::__1::__wrap_iter"** %2, align 8
  %4 = getelementptr inbounds %"class.std::__1::__wrap_iter", %"class.std::__1::__wrap_iter"* %3, i32 0, i32 0
  %5 = load i32*, i32** %4, align 8
  ret i32* %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) %"class.std::__1::__wrap_iter"* @_ZNSt3__111__wrap_iterIPjEppEv(%"class.std::__1::__wrap_iter"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::__wrap_iter"*, align 8
  store %"class.std::__1::__wrap_iter"* %0, %"class.std::__1::__wrap_iter"** %2, align 8
  %3 = load %"class.std::__1::__wrap_iter"*, %"class.std::__1::__wrap_iter"** %2, align 8
  %4 = getelementptr inbounds %"class.std::__1::__wrap_iter", %"class.std::__1::__wrap_iter"* %3, i32 0, i32 0
  %5 = load i32*, i32** %4, align 8
  %6 = getelementptr inbounds i32, i32* %5, i32 1
  store i32* %6, i32** %4, align 8
  ret %"class.std::__1::__wrap_iter"* %3
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden zeroext i1 @_ZNSt3__1eqIPjS1_EEbRKNS_11__wrap_iterIT_EERKNS2_IT0_EE(%"class.std::__1::__wrap_iter"* nonnull align 8 dereferenceable(8) %0, %"class.std::__1::__wrap_iter"* nonnull align 8 dereferenceable(8) %1) #1 {
  %3 = alloca %"class.std::__1::__wrap_iter"*, align 8
  %4 = alloca %"class.std::__1::__wrap_iter"*, align 8
  store %"class.std::__1::__wrap_iter"* %0, %"class.std::__1::__wrap_iter"** %3, align 8
  store %"class.std::__1::__wrap_iter"* %1, %"class.std::__1::__wrap_iter"** %4, align 8
  %5 = load %"class.std::__1::__wrap_iter"*, %"class.std::__1::__wrap_iter"** %3, align 8
  %6 = call i32* @_ZNKSt3__111__wrap_iterIPjE4baseEv(%"class.std::__1::__wrap_iter"* %5) #11
  %7 = load %"class.std::__1::__wrap_iter"*, %"class.std::__1::__wrap_iter"** %4, align 8
  %8 = call i32* @_ZNKSt3__111__wrap_iterIPjE4baseEv(%"class.std::__1::__wrap_iter"* %7) #11
  %9 = icmp eq i32* %6, %8
  ret i1 %9
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i64 @_ZNSt3__1miIPKjS2_EEDTmicldtfp_4baseEcldtfp0_4baseEERKNS_11__wrap_iterIT_EERKNS4_IT0_EE(%"class.std::__1::__wrap_iter.16"* nonnull align 8 dereferenceable(8) %0, %"class.std::__1::__wrap_iter.16"* nonnull align 8 dereferenceable(8) %1) #1 {
  %3 = alloca %"class.std::__1::__wrap_iter.16"*, align 8
  %4 = alloca %"class.std::__1::__wrap_iter.16"*, align 8
  store %"class.std::__1::__wrap_iter.16"* %0, %"class.std::__1::__wrap_iter.16"** %3, align 8
  store %"class.std::__1::__wrap_iter.16"* %1, %"class.std::__1::__wrap_iter.16"** %4, align 8
  %5 = load %"class.std::__1::__wrap_iter.16"*, %"class.std::__1::__wrap_iter.16"** %3, align 8
  %6 = call i32* @_ZNKSt3__111__wrap_iterIPKjE4baseEv(%"class.std::__1::__wrap_iter.16"* %5) #11
  %7 = load %"class.std::__1::__wrap_iter.16"*, %"class.std::__1::__wrap_iter.16"** %4, align 8
  %8 = call i32* @_ZNKSt3__111__wrap_iterIPKjE4baseEv(%"class.std::__1::__wrap_iter.16"* %7) #11
  %9 = ptrtoint i32* %6 to i64
  %10 = ptrtoint i32* %8 to i64
  %11 = sub i64 %9, %10
  %12 = sdiv exact i64 %11, 4
  ret i64 %12
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i32* @_ZNKSt3__16vectorIjNS_9allocatorIjEEE6cbeginEv(%"class.std::__1::vector"* %0) #1 align 2 {
  %2 = alloca %"class.std::__1::__wrap_iter.16", align 8
  %3 = alloca %"class.std::__1::vector"*, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %3, align 8
  %4 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  %5 = call i32* @_ZNKSt3__16vectorIjNS_9allocatorIjEEE5beginEv(%"class.std::__1::vector"* %4) #11
  %6 = getelementptr inbounds %"class.std::__1::__wrap_iter.16", %"class.std::__1::__wrap_iter.16"* %2, i32 0, i32 0
  store i32* %5, i32** %6, align 8
  %7 = getelementptr inbounds %"class.std::__1::__wrap_iter.16", %"class.std::__1::__wrap_iter.16"* %2, i32 0, i32 0
  %8 = load i32*, i32** %7, align 8
  ret i32* %8
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__16vectorIjNS_9allocatorIjEEE17__destruct_at_endEPj(%"class.std::__1::vector"* %0, i32* %1) #1 align 2 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %3 = alloca %"class.std::__1::vector"*, align 8
  %4 = alloca i32*, align 8
  %5 = alloca i64, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %3, align 8
  store i32* %1, i32** %4, align 8
  %6 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  %7 = load i32*, i32** %4, align 8
  invoke void @_ZNSt3__16vectorIjNS_9allocatorIjEEE27__invalidate_iterators_pastEPj(%"class.std::__1::vector"* %6, i32* %7)
          to label %8 unwind label %13

8:                                                ; preds = %2
  %9 = call i64 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE4sizeEv(%"class.std::__1::vector"* %6) #11
  store i64 %9, i64* %5, align 8
  %10 = bitcast %"class.std::__1::vector"* %6 to %"class.std::__1::__vector_base"*
  %11 = load i32*, i32** %4, align 8
  call void @_ZNSt3__113__vector_baseIjNS_9allocatorIjEEE17__destruct_at_endEPj(%"class.std::__1::__vector_base"* %10, i32* %11) #11
  %12 = load i64, i64* %5, align 8
  call void @_ZNKSt3__16vectorIjNS_9allocatorIjEEE17__annotate_shrinkEm(%"class.std::__1::vector"* %6, i64 %12) #11
  ret void

13:                                               ; preds = %2
  %14 = landingpad { i8*, i32 }
          catch i8* null
  %15 = extractvalue { i8*, i32 } %14, 0
  call void @__clang_call_terminate(i8* %15) #17
  unreachable
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr hidden i32* @_ZNSt3__14moveIPjS1_EET0_T_S3_S2_(i32* %0, i32* %1, i32* %2) #0 {
  %4 = alloca i32*, align 8
  %5 = alloca i32*, align 8
  %6 = alloca i32*, align 8
  store i32* %0, i32** %4, align 8
  store i32* %1, i32** %5, align 8
  store i32* %2, i32** %6, align 8
  %7 = load i32*, i32** %4, align 8
  %8 = call i32* @_ZNSt3__113__unwrap_iterIPjEET_S2_(i32* %7)
  %9 = load i32*, i32** %5, align 8
  %10 = call i32* @_ZNSt3__113__unwrap_iterIPjEET_S2_(i32* %9)
  %11 = load i32*, i32** %6, align 8
  %12 = call i32* @_ZNSt3__113__unwrap_iterIPjEET_S2_(i32* %11)
  %13 = call i32* @_ZNSt3__16__moveIjjEENS_9enable_ifIXaasr7is_sameINS_12remove_constIT_E4typeET0_EE5valuesr28is_trivially_copy_assignableIS6_EE5valueEPS6_E4typeEPS3_SA_S7_(i32* %8, i32* %10, i32* %12)
  ret i32* %13
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__16vectorIjNS_9allocatorIjEEE27__invalidate_iterators_pastEPj(%"class.std::__1::vector"* %0, i32* %1) #1 align 2 {
  %3 = alloca %"class.std::__1::vector"*, align 8
  %4 = alloca i32*, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %3, align 8
  store i32* %1, i32** %4, align 8
  %5 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNKSt3__16vectorIjNS_9allocatorIjEEE17__annotate_shrinkEm(%"class.std::__1::vector"* %0, i64 %1) #1 align 2 {
  %3 = alloca %"class.std::__1::vector"*, align 8
  %4 = alloca i64, align 8
  store %"class.std::__1::vector"* %0, %"class.std::__1::vector"** %3, align 8
  store i64 %1, i64* %4, align 8
  %5 = load %"class.std::__1::vector"*, %"class.std::__1::vector"** %3, align 8
  %6 = call i32* @_ZNKSt3__16vectorIjNS_9allocatorIjEEE4dataEv(%"class.std::__1::vector"* %5) #11
  %7 = bitcast i32* %6 to i8*
  %8 = call i32* @_ZNKSt3__16vectorIjNS_9allocatorIjEEE4dataEv(%"class.std::__1::vector"* %5) #11
  %9 = call i64 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE8capacityEv(%"class.std::__1::vector"* %5) #11
  %10 = getelementptr inbounds i32, i32* %8, i64 %9
  %11 = bitcast i32* %10 to i8*
  %12 = call i32* @_ZNKSt3__16vectorIjNS_9allocatorIjEEE4dataEv(%"class.std::__1::vector"* %5) #11
  %13 = load i64, i64* %4, align 8
  %14 = getelementptr inbounds i32, i32* %12, i64 %13
  %15 = bitcast i32* %14 to i8*
  %16 = call i32* @_ZNKSt3__16vectorIjNS_9allocatorIjEEE4dataEv(%"class.std::__1::vector"* %5) #11
  %17 = call i64 @_ZNKSt3__16vectorIjNS_9allocatorIjEEE4sizeEv(%"class.std::__1::vector"* %5) #11
  %18 = getelementptr inbounds i32, i32* %16, i64 %17
  %19 = bitcast i32* %18 to i8*
  call void @_ZNKSt3__16vectorIjNS_9allocatorIjEEE31__annotate_contiguous_containerEPKvS5_S5_S5_(%"class.std::__1::vector"* %5, i8* %7, i8* %11, i8* %15, i8* %19) #11
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i32* @_ZNSt3__16__moveIjjEENS_9enable_ifIXaasr7is_sameINS_12remove_constIT_E4typeET0_EE5valuesr28is_trivially_copy_assignableIS6_EE5valueEPS6_E4typeEPS3_SA_S7_(i32* %0, i32* %1, i32* %2) #1 {
  %4 = alloca i32*, align 8
  %5 = alloca i32*, align 8
  %6 = alloca i32*, align 8
  %7 = alloca i64, align 8
  store i32* %0, i32** %4, align 8
  store i32* %1, i32** %5, align 8
  store i32* %2, i32** %6, align 8
  %8 = load i32*, i32** %5, align 8
  %9 = load i32*, i32** %4, align 8
  %10 = ptrtoint i32* %8 to i64
  %11 = ptrtoint i32* %9 to i64
  %12 = sub i64 %10, %11
  %13 = sdiv exact i64 %12, 4
  store i64 %13, i64* %7, align 8
  %14 = load i64, i64* %7, align 8
  %15 = icmp ugt i64 %14, 0
  br i1 %15, label %16, label %23

16:                                               ; preds = %3
  %17 = load i32*, i32** %6, align 8
  %18 = bitcast i32* %17 to i8*
  %19 = load i32*, i32** %4, align 8
  %20 = bitcast i32* %19 to i8*
  %21 = load i64, i64* %7, align 8
  %22 = mul i64 %21, 4
  call void @llvm.memmove.p0i8.p0i8.i64(i8* align 4 %18, i8* align 4 %20, i64 %22, i1 false)
  br label %23

23:                                               ; preds = %16, %3
  %24 = load i32*, i32** %6, align 8
  %25 = load i64, i64* %7, align 8
  %26 = getelementptr inbounds i32, i32* %24, i64 %25
  ret i32* %26
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden i32* @_ZNSt3__113__unwrap_iterIPjEET_S2_(i32* %0) #1 {
  %2 = alloca i32*, align 8
  store i32* %0, i32** %2, align 8
  %3 = load i32*, i32** %2, align 8
  ret i32* %3
}

; Function Attrs: argmemonly nounwind willreturn
declare void @llvm.memmove.p0i8.p0i8.i64(i8* nocapture, i8* nocapture readonly, i64, i1 immarg) #8

attributes #0 = { noinline optnone ssp uwtable "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "frame-pointer"="all" "less-precise-fpmad"="false" "min-legal-vector-width"="0" "no-infs-fp-math"="false" "no-jump-tables"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="penryn" "target-features"="+cx16,+cx8,+fxsr,+mmx,+sahf,+sse,+sse2,+sse3,+sse4.1,+ssse3,+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #1 = { noinline nounwind optnone ssp uwtable "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "frame-pointer"="all" "less-precise-fpmad"="false" "min-legal-vector-width"="0" "no-infs-fp-math"="false" "no-jump-tables"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="penryn" "target-features"="+cx16,+cx8,+fxsr,+mmx,+sahf,+sse,+sse2,+sse3,+sse4.1,+ssse3,+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #2 = { "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "frame-pointer"="all" "less-precise-fpmad"="false" "no-infs-fp-math"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="penryn" "target-features"="+cx16,+cx8,+fxsr,+mmx,+sahf,+sse,+sse2,+sse3,+sse4.1,+ssse3,+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #3 = { cold noreturn "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="true" "frame-pointer"="all" "less-precise-fpmad"="false" "no-infs-fp-math"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="penryn" "target-features"="+cx16,+cx8,+fxsr,+mmx,+sahf,+sse,+sse2,+sse3,+sse4.1,+ssse3,+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #4 = { nobuiltin nounwind "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "frame-pointer"="all" "less-precise-fpmad"="false" "no-infs-fp-math"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="penryn" "target-features"="+cx16,+cx8,+fxsr,+mmx,+sahf,+sse,+sse2,+sse3,+sse4.1,+ssse3,+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #5 = { nounwind readnone speculatable willreturn }
attributes #6 = { nobuiltin allocsize(0) "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "frame-pointer"="all" "less-precise-fpmad"="false" "no-infs-fp-math"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="penryn" "target-features"="+cx16,+cx8,+fxsr,+mmx,+sahf,+sse,+sse2,+sse3,+sse4.1,+ssse3,+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #7 = { argmemonly nounwind willreturn writeonly }
attributes #8 = { argmemonly nounwind willreturn }
attributes #9 = { noinline noreturn nounwind }
attributes #10 = { noreturn "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "frame-pointer"="all" "less-precise-fpmad"="false" "no-infs-fp-math"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="penryn" "target-features"="+cx16,+cx8,+fxsr,+mmx,+sahf,+sse,+sse2,+sse3,+sse4.1,+ssse3,+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #11 = { nounwind }
attributes #12 = { nounwind "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "frame-pointer"="all" "less-precise-fpmad"="false" "no-infs-fp-math"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="penryn" "target-features"="+cx16,+cx8,+fxsr,+mmx,+sahf,+sse,+sse2,+sse3,+sse4.1,+ssse3,+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #13 = { noinline noreturn optnone ssp uwtable "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "frame-pointer"="all" "less-precise-fpmad"="false" "min-legal-vector-width"="0" "no-infs-fp-math"="false" "no-jump-tables"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="penryn" "target-features"="+cx16,+cx8,+fxsr,+mmx,+sahf,+sse,+sse2,+sse3,+sse4.1,+ssse3,+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #14 = { cold noreturn }
attributes #15 = { builtin nounwind }
attributes #16 = { builtin allocsize(0) }
attributes #17 = { noreturn nounwind }
attributes #18 = { noreturn }

!llvm.module.flags = !{!0, !1}
!llvm.ident = !{!2}

!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 7, !"PIC Level", i32 2}
!2 = !{!"Homebrew clang version 11.1.0"}
