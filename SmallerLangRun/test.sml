[TestModule]

//External function 
extern print :: (int: d)

//Enumeration
enum MyEnum
{
	Zero = 0
	One = 1
	Two
	Three
	Five = 5
}

//Basic struct
struct Test
{
	int: field = 5
}

//This struct inherits it's fields from Test
struct Test2 : Test 
{
	int: field2 = 2
	int: field3 = 3
}

//For implicit and explicit casts from Test to int
cast :: (Test: t) -> int
{
	return t.field
}

InitialMethod :: ()
{
	let t = CreateTest()
	print(** t)
	print(Multiply(t))
	
	let i, j, k = MultipleReturnTest()
	i, _, k += MultipleReturnTest2() //Discard the second value returned from MultipleReturnTest2
	
	print(i)
	print(j)
	print(k)
	
	let e = MyEnum.Five
	select(e)
	{
		case MyEnum.Zero
		
		case MyEnum.One
			print(9999)
			
		case MyEnum.Two, 
			 MyEnum.Three,
			 MyEnum.Five
		
	} @complete

	let x = [int: 5]
	defer PrintArray(x)
	
	for(let y = 0: y < lengthof x: y++)
	{
		x[y] = y
	}
	
	print(Product(x))
} @run

CreateTest :: () -> Test
{
	let t = new Test
	return t
}

MultipleReturnTest :: () -> int, int, int
{
	return 1, 2, 3
}

MultipleReturnTest2 :: () -> int, int, int
{
	return 2, 4, 6
}

Multiply :: (Test: t3) -> int
{
	return t3.field
}

PrintArray :: (int[]: array)
{
	for(array) print(it)
}

Product :: (int[]: x) -> int
{
	let product = 1
	defer print(product)
	
	for(x)
	{
		if(it == 0) it = 1	
		product *= it
	}
	return product
}